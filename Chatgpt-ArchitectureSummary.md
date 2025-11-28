Below is a **clean, consolidated summary** of the entire EC (Eligibility / Benefits Checker) architecture we designed in this chat, including:

* Domain + architectural goals
* DTO shapes (record-based)
* Controllers (middleware-driven exception handling)
* Service contracts (strict exception strategy)
* Repository interfaces
* Cosmos-backed repository patterns
* Entity shapes
* Infrastructure patterns (Cosmos partitioning, paging, lookups, config)

This represents the *current state of the design*.

---

# ✅ **1. Overall Architecture Goals**

1. **Multi-tenant healthcare SaaS** for Optometry with future extensibility to Ophthalmology & Orthodontics.
2. **HIPAA-compliant API** using:

   * No PHI in URLs or logs
   * All sensitive data passed in **request bodies**
3. **Clean separation**:

   * **Controllers** → no try/catch, no null checks
   * **Services** → enforce validation through **exceptions**
   * **Repositories** → return `null` for missing entities
   * **Middleware** → maps exceptions → HTTP status codes
4. **Cosmos DB Serverless** storage with:

   * One partition key: **tenantId** across all containers
   * Continuation-token based paging
   * Per-container indexing tuned for RUs
5. **EC (Eligibility Checker)** integrates with clearinghouses like **Availity**.

---

# ✅ **2. Standard Exception Strategy**

Services throw:

* `ArgumentException` → **400**
* `UnauthorizedAccessException` → **401**
* `KeyNotFoundException` → **404**
* Any other exception → **500**

Controllers do **not** catch exceptions.
Middleware handles everything.

---

# ✅ **3. DTOs (Record-Based)**

All DTOs are **C# record types**, examples:

```csharp
public record PatientSearchRequestDto(
    string? SearchText,
    int Page,
    int PageSize,
    string? ContinuationToken
);

public record PatientSearchResultDto(
    string PatientId,
    string FirstName,
    string LastName,
    DateTime? DateOfBirth
);

public record PatientDetailDto(
    string PatientId,
    string FirstName,
    string LastName,
    DateTime? DateOfBirth,
    string? Email,
    string? Phone
);

public record CoverageEnrollmentDto(
    string CoverageEnrollmentId,
    string PayerId,
    string MemberId,
    string GroupNumber,
    string CoverageType,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo
);
```

(similar record DTOs exist for Encounters, Eligibility Checks, Lookups, Config settings, etc.)

---

# ✅ **4. Controllers (Middleware-Driven)**

Controllers:

* Use `[ApiController]`, `[Authorize]`, and route prefixes.
* Extract tenantId: `User.GetTenantIdOrThrow()`.
* Do not contain: try/catch, null checks, boolean logic.

**Example: PatientsController**

```csharp
[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController : ControllerBase, IPatientsController
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [HttpPost("search")]
    public async Task<ActionResult<PagedResult<PatientSearchResultDto>>> SearchPatientsAsync(
        [FromBody] PatientSearchRequestDto request)
    {
        var tenantId = User.GetTenantIdOrThrow();
        var result = await _patientService.SearchPatientsAsync(tenantId, request);
        return Ok(result);
    }

    [HttpGet("{patientId}")]
    public async Task<ActionResult<PatientDetailDto>> GetPatientAsync(string patientId)
    {
        var tenantId = User.GetTenantIdOrThrow();
        var patient = await _patientService.GetPatientAsync(tenantId, patientId);
        return Ok(patient);
    }

    [HttpPost]
    public async Task<ActionResult<PatientDetailDto>> CreatePatientAsync(
        [FromBody] PatientCreateRequestDto request)
    {
        var tenantId = User.GetTenantIdOrThrow();
        var created = await _patientService.CreatePatientAsync(tenantId, request);

        return CreatedAtAction(nameof(GetPatientAsync),
            new { patientId = created.PatientId }, created);
    }

    // … updates, coverage enrollment, encounter searches, etc.
}
```

All other controllers follow this same template (Encounters, Payers, Practices, Lookups, Config, Session).

---

# ✅ **5. Service Interfaces (Strict Contracts)**

Services enforce correctness and security via exceptions.

Example: `IPatientService`:

```csharp
public interface IPatientService
{
    Task<PagedResult<PatientSearchResultDto>> SearchPatientsAsync(
        string tenantId, PatientSearchRequestDto request);

    Task<PatientDetailDto> GetPatientAsync(
        string tenantId, string patientId);

    Task<PatientDetailDto> CreatePatientAsync(
        string tenantId, PatientCreateRequestDto request);

    Task<PatientDetailDto> UpdatePatientAsync(
        string tenantId, string patientId, PatientUpdateRequestDto request);

    Task<CoverageEnrollmentDto> AddCoverageEnrollmentAsync(
        string tenantId, string patientId, CoverageEnrollmentCreateRequestDto request);

    Task<CoverageEnrollmentDto> UpdateCoverageEnrollmentAsync(
        string tenantId, string patientId, string coverageEnrollmentId, 
        CoverageEnrollmentUpdateRequestDto request);

    Task DeleteCoverageEnrollmentAsync(
        string tenantId, string patientId, string coverageEnrollmentId);

    Task<PagedResult<EncounterSummaryDto>> GetPatientEncountersAsync(
        string tenantId, string patientId, PatientEncounterSearchRequestDto request);
}
```

All other services follow the same rule:

* No booleans to indicate success/failure
* No null returns
* Everything validated by throwing

---

# ✅ **6. Service Implementations (Patterns)**

Service implementations:

* Validate required parameters → throw `ArgumentException`
* Check access → throw `UnauthorizedAccessException`
* Fetch entity from repo:

  * `null` → throw `KeyNotFoundException`
* Update entity and save via repo

**Example: PatientService.GetPatientAsync()**

```csharp
var entity = await _patientRepo.GetByIdAsync(tenantId, patientId);
if (entity is null)
    throw new KeyNotFoundException("Patient not found.");

return MapToDetailDto(entity);
```

This pattern repeats across all services.

---

# ✅ **7. Repository Interfaces**

Repositories return **null**, never throw for missing items.

Examples:

```csharp
public interface IPatientRepository
{
    Task<PatientEntity?> GetByIdAsync(string tenantId, string patientId);
    Task CreateAsync(PatientEntity entity);
    Task UpdateAsync(PatientEntity entity);
    Task<PagedResult<PatientSearchResultDto>> SearchAsync(
        string tenantId, PatientSearchRequestDto request);
}

public interface ICoverageRepository { … }
public interface IEncounterRepository { … }
public interface IPracticeRepository { … }
public interface IPayerRepository { … }
public interface ILookupRepository { … }
public interface IConfigRepository { … }
```

Repositories are thin wrappers over Cosmos.

---

# ✅ **8. Cosmos Repositories (Implementation Sketch)**

All Cosmos repos:

* Use container per entity type
* Partition by `/tenantId`
* All item reads use: `ReadItemAsync(id, new PartitionKey(tenantId))`
* Searches use SQL queries + continuation tokens.

**Example: CosmosPatientRepository.SearchAsync()**

```csharp
var iterator = _container.GetItemQueryIterator<PatientEntity>(
    queryDef,
    continuationToken: request.ContinuationToken,
    requestOptions: new QueryRequestOptions {
        PartitionKey = new PartitionKey(tenantId),
        MaxItemCount = request.PageSize
    });

var items = new List<PatientSearchResultDto>();
string? newToken = null;

if (iterator.HasMoreResults)
{
    var resp = await iterator.ReadNextAsync();
    newToken = resp.ContinuationToken;

    items.AddRange(resp.Resource.Select(e =>
        new PatientSearchResultDto(e.Id, e.FirstName, e.LastName, e.DateOfBirth)));
}

return new PagedResult<PatientSearchResultDto>(items, request.Page, request.PageSize, null, newToken);
```

**Other repos** follow the same pattern:

* Practices → simple list queries
* Payers → optional WHERE clause filters
* Coverage → read/update/delete by id
* Encounters → depends on container organization
* Eligibility Checks → separate container
* Config → tenant-level config by id = tenantId
* Lookups → static or per-tenant config items

---

# ✅ **9. Entity Shapes**

Entities follow Cosmos document structure, e.g.:

```csharp
public class PatientEntity
{
    public string Id { get; set; } = default!;
    public string TenantId { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public DateTime? DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string patientContainerId => Id; // mirror ID convention
}
```

Other entities mirror this layout:

* `CoverageEnrollmentEntity`
* `EncounterEntity`
* `EligibilityCheckEntity`
* `PracticeEntity`
* `PayerEntity`
* `TenantConfigEntity`
* `PayerConfigEntity`

---

# ✅ **10. Cosmos Index Policies (Optimized)**

You drafted index policies to reduce RU costs:

### Patients container

* Index: `/firstName/?`, `/lastName/?`
* Exclude `RawEligibilityResponse`, `Notes`, etc.
* Range indexes on DOB for date search

### Encounters container

* Index: `visitDate`, `visitTypeCode`, `patientId`
* Exclude large payload sections

### EligibilityChecks container

* Index minimal fields: `encounterId`, `requestTimestampUtc`

### Config + Lookups containers

* Mostly index everything (tiny documents)

---

# ✅ **11. Global Exception Middleware**

Middleware handles *all* exceptions:

* Maps them to HTTP codes
* Logs without PHI
* Ensures consistent output:

```json
{
  "error": "NotFound",
  "message": "The requested resource was not found.",
  "correlationId": "..."
}
```

Controllers therefore contain no error handling logic.

---

# ⭐ **12. Final Architecture Summary**

Your EC system is now:

* **HIPAA-safe**
* **Cosmos-optimized**
* **Multi-tenant**
* **Extensible to multiple specialties**
* **Strict contracts + clean layering**
* **Blazor-ready backend**

And you now have:

* DTOs (records)
* Controllers (middleware-driven)
* Services (exception strategy)
* Repositories (interface & Cosmos implementations)
* Entity models
* Indexing policies
* Paging strategy
* Claims-based tenant scoping

---

If you'd like, I can generate a **final high-level architecture diagram**, a **folder structure**, or full **Program.cs + DI wiring** for all repos/services.
