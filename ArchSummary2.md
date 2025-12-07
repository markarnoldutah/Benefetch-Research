Here’s an **updated EC (Eligibility / Benefits Checker) architecture summary** that reflects the *actual* code in `API.zip`, `Domain.zip`, `Repositories.zip`, and `seed-program.cs` (overriding the older write-up where they conflict).  

---

## 1. High-Level Architecture & Goals

**Purpose**

A multi-tenant, cloud-native API for an Optometry-focused Eligibility & Benefits Checker (EC). It’s designed as a **front-desk co-pilot** that:

* Stores patients, coverage enrollments, encounters, and embedded eligibility checks
* Applies tenant + payer configuration (COB rules, encounter types, clearinghouse behaviors)
* Exposes a clean REST API for a Blazor or other client front-end

**Key goals (still aligned with the original doc):** 

* Multi-tenant, HIPAA-aware design (no PHI in URLs or logs; all sensitive data in request bodies)
* Clear layering: **API → Services → Repositories → Cosmos**
* Cosmos DB Serverless for bursty workloads
* Payer + tenant config modeled as documents, not hard-coded rules
* Lookup system that’s **global-first** with future tenant overrides

---

## 2. Projects & Layers

### 2.1 API project (`API`)

**Responsibilities**

* ASP.NET Core Web API surface
* Controllers for:

  * `PatientsController`
  * `EncountersController`
  * `PracticesController`
  * `PayersController`
  * `ConfigController` (tenant & payer config)
  * `LookupsController`
  * `SessionController` (session context & roles)
  * `WeatherForecastController` (placeholder)
* Middleware:

  * `ExceptionHandlingMiddleware` for centralized error handling
* Mapping layer:

  * `PatientMapper`, `EncounterMapper`, `PracticeMapper`, `PayerMapper`, `ConfigMapper`, `LookupMapper`
* Auth & session:

  * Claims extensions (`ClaimsPrincipalExtensions`) to extract `tenantId` from JWT claims

**Pattern**

* Controllers are annotated with `[ApiController]` & `[Authorize]`
* Controllers:

  * Pull `tenantId` from claims via `User.GetTenantIdOrThrow()`
  * Call **Domain services** (`IPatientService`, `IEncounterService`, etc.)
  * Map **entities → DTOs** at the boundary using `*Mapper` classes
* No try/catch in controllers; errors are handled by middleware.

---

### 2.2 Domain project (`Domain`)

**Responsibilities**

* **Entities**: Cosmos document shapes (`Tenant`, `TenantConfig`, `PayerConfig`, `Practice`, `Patient`, `Encounter`, `LookupSet`, etc.)
* **Embedded value objects**: `CoverageEnrollmentEmbedded`, `CoverageDecisionEmbedded`, `EligibilityCheckEmbedded`, `CoverageLineEmbedded`, `EligibilityPayloadEmbedded`, etc.
* **DTOs**: `PatientDetailDto`, `EncounterDetailDto`, `TenantConfigUpdateRequestDto`, `PayerConfigUpdateRequestDto`, `LookupSetDto`, etc.
* **Interfaces**:

  * Services: `IPatientService`, `IEncounterService`, `IPayerService`, `IPracticeService`, `ILookupService`, `IConfigService`, `ISessionService`
  * Repositories: `IPatientRepository`, `IEncounterRepository`, `IPayerRepository`, `IPracticeRepository`, `ILookupRepository`, `IConfigRepository`, `ITenantAccessRepository`
  * Cross-cutting extension points: `IAuditRepository`, `IBlobRepository`, `ITelemetryService`
* **Shared utilities**: `PagedResult<T>`, date/time helpers, `LookupOverrideMode` enum.

> **Important code reality:**
> Domain **services return entities, not DTOs**. Controllers call mappers to convert entities into DTOs at the API boundary. This is a key difference from the original summary (which had services returning DTOs directly). 

---

### 2.3 Repositories project (`Repositories`)

**Responsibilities**

* Cosmos DB implementations for Domain repository interfaces:

  * `CosmosPatientRepository`
  * `CosmosEncounterRepository`
  * `CosmosPracticeRepository`
  * `CosmosPayerRepository`
  * `CosmosLookupRepository`
  * `CosmosConfigRepository` (tenant + payer config)
  * `CosmosTenantAccessRepository` (tenant access / roles)
* Shared base:

  * `CosmosRepositoryBase` (wraps `CosmosClient` and exposes `GetContainer()` helper)

Repositories are thin, focused on:

* Partition key usage
* SQL queries (`QueryDefinition`)
* Mapping raw Cosmos results into Domain entities

---

## 3. Data Model & Tenancy

### 3.1 Base Entity & Tenancy

All Cosmos entities derive from `EntityBase`:

* `Type` (string, discriminator, e.g., `"patient"`, `"tenantConfig"`)
* `Id` (Cosmos `id`, GUID string by default)
* `TenantId` (string?, partition key for most containers)
* `Name` (optional display name)
* `IsEnabled` (bool)
* `CreatedAtUtc` (DateTime, init-only)
* `CreatedByUserId` (string?)

Tenancy strategy:

* **Per-tenant data**: `TenantId` = actual tenant id (e.g. `"ten_001"`), container partitioned by `/tenantId`
* **Global data**: `TenantId` is either:

  * `null` (for truly global payers), or
  * `"GLOBAL"` for global lookup sets (MVP)

---

### 3.2 Cosmos Containers (from `seed-program.cs`) 

Database: `bfdb`

Containers:

1. **Tenants** (`tenants`) – PK: `/tenantId`

   * Holds:

     * `Tenant` docs (`Type = "tenant"`)
     * `TenantConfig` docs (`Type = "tenantConfig"`)
     * `PayerConfig` docs (`Type = "payerConfig"`)
   * All partitioned by `TenantId`

2. **Practices** (`practices`) – PK: `/tenantId`

   * `Practice` docs; each practice may have `Locations` collection

3. **Patients** (`patients`) – PK: `/tenantId`

   * `Patient` docs with embedded `CoverageEnrollments`

4. **Encounters** (`encounters`) – PK: `/tenantId`

   * `Encounter` docs with embedded `CoverageDecision` and list of `EligibilityChecks`

5. **Payers** (`payers`) – **PK: `/id`** (important!)

   * Mostly global payer master data:

     * Some with `TenantId = null` (pure master)
     * Some tenant-specific (e.g., Medicaid for a state/tenant)

6. **Lookups** (`lookups`) – PK: `/tenantId`

   * `LookupSet` docs (global or tenant-specific in the future)
   * For MVP, only `TenantId = "GLOBAL"` is used.

> This differs from the original summary which assumed **all** containers were partitioned by `/tenantId`, and which placed tenant/payer config in a separate “Config” container. In the actual code + seed, config docs live in the **Tenants** container with a `Type` discriminator.  

---

## 4. DTOs & Mapping

### 4.1 DTO Style

DTOs are defined as **C# `record` types with property initializers**, not positional records. For example:

```csharp
public record PatientSearchRequestDto
{
    public string? PracticeId { get; init; }
    public string? LastName { get; init; }
    public string? FirstName { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? MemberId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public string? SearchText { get; set; }
    public string ContinuationToken { get; set; } 
}
```

Paged results:

```csharp
public record PagedResult<T>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long TotalCount { get; init; }
    public List<T> Items { get; init; } = new();
}
```

> The original doc had `PagedResult` carrying a continuation token; the **current code does not**. Continuation token handling, if any, is internal to repositories; the DTO exposed to clients is page- and total-count based. 

### 4.2 Mapping at the API Boundary

Mapping is explicitly handled in **API Mappers**, not inside services:

* `PatientMapper`:

  * `Patient → PatientDetailDto`
  * `Patient → PatientSearchResultDto`
  * `PagedResult<Patient> → PagedResult<PatientSearchResultDto>`
* `EncounterMapper`, `PracticeMapper`, `PayerMapper`, `ConfigMapper`, `LookupMapper` follow the same pattern.

This keeps:

* **Domain services** returning **entities**
* **Controllers** dealing only with DTOs

---

## 5. Services Layer (Domain Interfaces + API Implementations)

### 5.1 Service Interfaces (Domain)

Examples:

```csharp
public interface IPatientService
{
    Task<PagedResult<Patient>> SearchPatientsAsync(string tenantId, PatientSearchRequestDto request);
    Task<Patient> GetPatientAsync(string tenantId, string patientId);
    Task<Patient> CreatePatientAsync(string tenantId, PatientCreateRequestDto request);
    Task<Patient> UpdatePatientAsync(string tenantId, string patientId, PatientUpdateRequestDto request);

    Task<CoverageEnrollmentEmbedded> AddCoverageEnrollmentAsync(string tenantId, string patientId, CoverageEnrollmentCreateRequestDto request);
    Task<CoverageEnrollmentEmbedded> UpdateCoverageEnrollmentAsync(string tenantId, string patientId, string coverageEnrollmentId, CoverageEnrollmentUpdateRequestDto request);
    Task DeleteCoverageEnrollmentAsync(string tenantId, string patientId, string coverageEnrollmentId);

    Task<PagedResult<Encounter>> GetPatientEncountersAsync(string tenantId, string patientId, PatientEncounterSearchRequestDto request);
}
```

Other services are similar:

* `IEncounterService` – encounter CRUD, eligibility check initiation, etc.
* `IPracticeService` – list practices & locations for a tenant.
* `IPayerService` – reads payer master + tenant-specific payer config.
* `IConfigService` – read/update `TenantConfig` and `PayerConfig`.
* `ILookupService` – read `LookupSet`s (with future tenant override rules).
* `ISessionService` – builds a `UserSessionContextDto` from claims and tenant access.

### 5.2 Service Implementations (API/Services)

The concrete implementations (e.g. `PatientService`, `EncounterService`, `ConfigService`, etc.) live in `API/Services` and:

* Accept a `tenantId` string (from claims) and DTOs or ids
* Use repositories to fetch/modify **entities**
* Enforce validation and business rules by **throwing exceptions** (e.g. `ArgumentException`, `UnauthorizedAccessException`, `KeyNotFoundException`)
* Return **entities** to be mapped by the API layer

> This matches the original exception-driven service strategy, but with the clarified entity-return pattern. 

---

## 6. Repositories & Cosmos Details

### 6.1 Base Class

`CosmosRepositoryBase`:

* Holds a `CosmosClient`
* Provides `GetContainer(string databaseId, string containerId)` with argument validation

### 6.2 Per-entity Repositories

* `CosmosPatientRepository`:

  * Partitioned by `tenantId`
  * Validates `TenantId` & `Id` before writes
  * Uses queries like:

    ```csharp
    var sql = "SELECT * FROM c WHERE c.tenantId = @tenantId";
    ```

    with optional `CONTAINS` filters on `firstName` / `lastName` if `SearchText` is provided.
  * Returns full `Patient` entities and manipulates the embedded `CoverageEnrollments` list.

* `CosmosEncounterRepository`:

  * Partitioned by `tenantId`
  * Stores `Encounter` with embedded COB decision and list of `EligibilityChecks`.
  * Query methods (search by patient, date, type, etc.)

* `CosmosPracticeRepository`:

  * Reads practices and their `Locations` by tenant.

* `CosmosConfigRepository`:

  * Targets `tenants` container:

    * `GetTenantConfigAsync(tenantId)` reads doc where `Type = "tenantConfig"` & `TenantId = tenantId`
    * `SaveTenantConfigAsync` upserts the `TenantConfig`
    * `GetPayerConfigsAsync` / `GetPayerConfigAsync` / `SavePayerConfigAsync` operate on `PayerConfig` docs, partitioned by `TenantId`.

* `CosmosLookupRepository`:

  * Targets `lookups` container:

    * Global sets: `TenantId = "GLOBAL"`
    * Future tenant overrides: `TenantId = actual tenant` with merge rules defined by `LookupOverrideMode`.

* `CosmosTenantAccessRepository`:

  * Exposes `HasAccessAsync(userId, tenantId)` (currently a stub/simple implementation).
  * `GetRolesForUser(ClaimsPrincipal)` reads roles from `ClaimTypes.Role`.

* `CosmosPayerRepository`:

  * Accesses the `payers` container.
  * Reads a `Payer` by id and a partition key.
  * **Important alignment note:** the seed script creates `payers` with `PartitionKeyPath = "/id"`, but the repository currently uses `PartitionKey(tenantId)` for some operations. This is an implementation mismatch that should be corrected (either change PK to `/tenantId` or adjust repo to use `id` as the partition key when reading). 

### 6.3 Indexing

* The **current code** does **not** explicitly set custom indexing policies.
* Containers created in `seed-program.cs` rely on Cosmos defaults; the earlier architecture doc’s “fine-tuned index policies” (e.g., excluding large fields) are **design intent**, not yet implemented in the seeding/bootstrap code.  

---

## 7. Config & Lookups Model (Current Implementation)

### 7.1 TenantConfig

`TenantConfig` (Type `"tenantConfig"`) includes: 

* `PracticeSettings`

  * `DefaultPracticeId`
* `EncounterSettings`

  * `DefaultRoutineEncounterTypeCode`
  * `DefaultMedicalEncounterTypeCode`
  * List of `EncounterTypeConfig` (code, display name, flags for routine/medical, allowed coverage types, default coverage type)
* `EligibilitySettings`

  * `EnableEligibilityChecks`
  * `EnableVisionPayerChecks`
  * `EnableMedicalPayerChecks`
  * `PrimaryClearinghouseCode`
  * `RequestTimeoutSeconds`
  * List of `PayerEligibilityBehaviorConfig` (supports real-time, vision vs medical, subscriber requirements, etc.)
* `CobSettings`

  * `RoutineExamPriority` (e.g. `"VisionThenMedical"`)
  * `MedicalVisitPriority` (e.g. `"MedicalThenVision"`)
* `UiSettings`

  * toggles for `ShowCoverageTab`, `ShowEncountersTab`, `ShowEligibilityHistoryTab`
  * `RequireEligibilityBeforeEncounter`
  * `AllowBypassEligibilityWithWarning`

### 7.2 PayerConfig

`PayerConfig` (Type `"payerConfig"`) provides **per-tenant payer enablement & ordering**:

* `PayerId`
* `PracticeId` (optional, for practice-specific configuration)
* `IsEnabled`
* `SortOrder`
* `DisplayName`
* `CobDefaultRole` (e.g., `"PrimaryVision"`, `"PrimaryMedical"`, `"SecondaryMedical"`)

The **Config API** (`ConfigController` + `ConfigService` + `CosmosConfigRepository`) exposes:

* Get/update `TenantConfig`
* Get/update list of `PayerConfig` records for a tenant

### 7.3 Lookups

`LookupSet` (Type `"lookupset"`) in the `lookups` container:

* `Category`, `Name`, `Description`
* `OverrideMode` (`GlobalOnly` for MVP)
* List of `LookupItem` (Code, Name, Description, SortOrder, IsActive)
* `TenantId = "GLOBAL"` for all MVP sets

Seeded examples: `service-types`, `visit-types`, `visit-reasons` (routine vs medical vs contact lens, pediatric, diabetic, etc.) 

---

## 8. API Surface & Error Handling

### 8.1 Controllers

Each main domain concept has a controller (e.g. `PatientsController`, `EncountersController`, `PracticesController`, `PayersController`, `ConfigController`, `LookupsController`, `SessionController`) that:

* Is `[Authorize]` (except where explicitly disabled)
* Reads `tenantId` from claims
* Interacts only with **services + DTOs**
* Uses mapper classes to convert from entities to DTOs

Endpoints include:

* `POST /api/patients/search`
* `GET /api/patients/{patientId}`
* `POST /api/patients` / `PUT /api/patients/{patientId}`
* Coverage enrollment operations under `/api/patients/{patientId}/coverage...`
* Encounter search & detail under `/api/encounters`
* Config endpoints under `/api/config/tenant` and `/api/config/payers`
* Lookups under `/api/lookups/{id}` or similar
* Session context: `GET /api/session/context`

### 8.2 ExceptionHandlingMiddleware

Custom middleware:

* Logs exceptions with tenant context (where available)

* Maps exceptions to HTTP status:

  * `UnauthorizedAccessException` → 401
  * `KeyNotFoundException` → 404
  * `ArgumentException` & likely other validation errors → 400
  * All other unhandled exceptions → 500

* Emits a JSON payload roughly like:

  ```json
  {
    "error": "BadRequest|NotFound|Unauthorized|ServerError",
    "message": "Human-readable error message",
    "correlationId": "guid-or-trace-id"
  }
  ```

> This matches the earlier architecture intent (middleware-based error handling, no try/catch in controllers) and is implemented via `ExceptionHandlingMiddleware : IMiddleware` in the API project. 

---

## 9. Security, Auth, and Session Context

* **Auth**: JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`)
* **Tenant binding**:

  * `ClaimsPrincipalExtensions.GetTenantIdOrThrow()` looks for:

    * `http://benefetch.com/tenantId`, then
    * fallback `tid`
  * Throws `UnauthorizedAccessException` if missing
* **SessionService**:

  * Uses `ITenantAccessRepository.HasAccessAsync(userId, tenantId)` to ensure the user can access the tenant
  * Uses `GetRolesForUser(ClaimsPrincipal)` to populate `Roles`
  * Returns `UserSessionContextDto` with:

    * TenantId
    * UserId
    * DisplayName (from identity)
    * Roles

`SessionController` exposes this as `GET /api/session/context`, allowing clients (e.g., a Blazor front-end) to bootstrap UI based on roles & tenant context.

---

## 10. Seed Program & Environment

`seed-program.cs` is a **standalone console seeder** used to:

* Create database & containers (if not exist) with the partition keys defined above
* Seed:

  * Tenants (`Tenant`)
  * Tenant configs (`TenantConfig`)
  * Payer configs (`PayerConfig`)
  * Practices + nested locations (`Practice`)
  * Payers (`Payer`)
  * Patients + embedded coverage enrollments (`Patient`)
  * Encounters + embedded coverage decisions + eligibility checks (`Encounter`)
  * Lookup sets (`LookupSet`)

The seed data provides **rich, realistic scaffolding**:

* Dual coverage scenarios (vision + medical)
* Medicare + Medigap
* Pediatric vision coverage
* Inactive coverage with failed eligibility
* Multiple encounter types (routine exam, medical eye visit, contact lens fitting)

This is all aligned with the business positioning of EC for optometry.  

---

## 11. Known Alignment Notes vs Original Architecture Doc

Compared to the original `Chatgpt-ArchitectureSummary.txt`, the **actual codebase** reflects these key differences/clarifications:  

1. **Services return entities**

   * Domain service interfaces (`IPatientService`, etc.) return **entities**, not DTOs. Mapping happens in API mappers.

2. **Config & Tenants in a single container**

   * `Tenant`, `TenantConfig`, and `PayerConfig` share the **Tenants** container, distinguished by `Type`.
   * There is no separate “Config” container.

3. **Payers container PK**

   * `payers` is created with `PartitionKeyPath = "/id"`.
   * `CosmosPayerRepository` needs to be aligned with this (currently uses `PartitionKey(tenantId)` in some places).

4. **Indexing policies**

   * No custom indexing policies are applied in the seed program; Cosmos defaults are currently used.

5. **DTO style & PagedResult**

   * DTOs are property-based `record`s.
   * `PagedResult<T>` does **not** expose continuation tokens; pagination is (at least externally) page & total-count based.

6. **Session & TenantAccess**

   * Session context and tenant access are now clearly represented by `ISessionService` and `ITenantAccessRepository` / `CosmosTenantAccessRepository`.

---

If you’d like, next I can:

* Pull out a **UML-style diagram** of entities & containers,
* Or create a **markdown architecture diagram** you can drop straight into docs / a README.
