# EC (Eligibility Checker) – Updated Architecture Summary
Updated to match the current implementation in **API.zip**, **Domain.zip**, **Repositories.zip**, and **seed-program.cs**.  
All conflicts with the older "ChatGPT Architecture Summary" are resolved in favor of the real code.

---

# 1. High-Level Architecture & Goals

The EC system is a cloud-native, multi-tenant, optometry-focused API designed as a **front-desk co-pilot** for insurance eligibility & benefits verification.

Key goals:

- Multi-tenant, HIPAA-aware design  
- Clean separation of layers: **API → Services → Repositories → Cosmos**  
- Cosmos DB Serverless for unpredictable + bursty workloads  
- Payer + tenant configuration stored as documents  
- Lookup system designed for global-first usage with future tenant overrides  
- Blazor-friendly API surface

---

# 2. Projects & Layers

## 2.1 API Project

Responsible for:

- Controllers (Patients, Encounters, Practices, Payers, Config, Lookups, Session)
- ExceptionHandlingMiddleware
- Claims extensions for tenant binding
- Mapping: Entities → DTOs

Controller pattern:

- Extract tenantId via `User.GetTenantIdOrThrow()`
- Call **services**
- Map entities to DTOs via API mappers
- No try/catch; all errors handled by middleware

---

## 2.2 Domain Project

Contains:

- **Entities:** Tenant, TenantConfig, PayerConfig, Practice, Patient, Encounter, LookupSet, etc.  
- **Embedded value objects:** CoverageEnrollment, EligibilityCheck, CoverageDecision, CoverageLine, EligibilityPayload  
- **DTOs:** PatientDetailDto, EncounterDetailDto, TenantConfigUpdateRequestDto, PayerConfigUpdateRequestDto, LookupSetDto…  
- **Interfaces:**  
  - Services: IPatientService, IEncounterService, IPayerService, etc.  
  - Repositories: IPatientRepository, IEncounterRepository, IPayerRepository…  
- **Shared utilities:** PagedResult<T>, LookupOverrideMode, date helpers

**Important:** Domain services return **entities**, not DTOs. Mapping occurs in the API layer.

---

## 2.3 Repositories Project

Contains Cosmos implementations for:

- CosmosPatientRepository  
- CosmosEncounterRepository  
- CosmosPracticeRepository  
- CosmosPayerRepository  
- CosmosLookupRepository  
- CosmosConfigRepository  
- CosmosTenantAccessRepository  

Repositories manage:

- Partition key enforcement  
- Cosmos SQL queries  
- Mapping Cosmos JSON → Domain entities  

---

# 3. Data Model & Tenant Strategy

## 3.1 Base Entity

All documents derive from EntityBase with:

- `Type` (discriminator)
- `Id`
- `TenantId` (partition key for most containers)
- `IsEnabled`
- `CreatedAtUtc`
- `CreatedByUserId`

## 3.2 Cosmos Containers (from seed-program.cs)

Database: `bfdb`

| Container | Partition Key | Contents |
|----------|----------------|----------|
| **tenants** | `/tenantId` | Tenant, TenantConfig, PayerConfig |
| **practices** | `/tenantId` | Practice + nested Locations |
| **patients** | `/tenantId` | Patient + embedded CoverageEnrollments |
| **encounters** | `/tenantId` | Encounter + embedded COB + EligibilityChecks |
| **payers** | `/id` | Global payer master data |
| **lookups** | `/tenantId` | Lookup sets (GLOBAL + future per-tenant) |

This differs from the old architecture summary, which assumed a separate "config" container.

---

# 4. DTOs & Mapping

DTOs use C# **property-init records**, not positional records.

`PagedResult<T>` includes:

- Page  
- PageSize  
- TotalCount  
- Items  

**Public API does not expose continuation tokens**.

Mapping happens in API layer using:

- PatientMapper  
- EncounterMapper  
- PracticeMapper  
- PayerMapper  
- ConfigMapper  
- LookupMapper  

---

# 5. Services Layer

Domain service interfaces define:

- CRUD operations for patients, encounters, practices  
- Coverage enrollment addition, update, deletion  
- Eligibility-check retrieval  
- Tenant and payer config operations  
- Session context construction  

Services:

- Accept tenantId from controllers  
- Validate input and existence  
- Throw exceptions (`ArgumentException`, `KeyNotFoundException`, `UnauthorizedAccessException`)  
- Return entities → mapped to DTOs by controllers  

---

# 6. Repositories & Cosmos Behavior

## Patterns followed:

- Validate partition key (tenantId or id)
- Add/Update via UpsertItemAsync
- Query via QueryDefinition  
- Map Cosmos JSON → Entities  

### Known mismatch:

`payers` container uses **PK = /id**, but repository sometimes uses tenantId.  
This is a required cleanup item.

---

# 7. Tenant Config, Payer Config, Lookups

## 7.1 TenantConfig

Stored with `Type = "tenantConfig"` in tenants container.  
Includes:

- PracticeSettings  
- EncounterSettings (encounter types, allowed coverage types, defaults)  
- EligibilitySettings (payers, clearinghouse rules)  
- CobSettings (VisionThenMedical, etc.)  
- UiSettings (tabs shown, eligibility enforcement)  

## 7.2 PayerConfig

Also in the tenants container:

- Enables/Disables a payer for a tenant  
- Sort order  
- Optional practice-level overrides  
- COB default roles  

## 7.3 Lookups

Stored in lookups container:

- Global sets: service types, visit types, visit reasons  
- `OverrideMode = GlobalOnly` for MVP  

---

# 8. API Surface & Error Handling

Controllers expose:

- Patient search, get, create, update  
- Coverage enrollment create/update/delete  
- Encounter search and detail  
- Config read/update  
- Lookup read  
- Session context  

### Error handling:

`ExceptionHandlingMiddleware` converts exceptions to:

| Exception | HTTP |
|----------|------|
| UnauthorizedAccessException | 401 |
| KeyNotFoundException | 404 |
| ArgumentException | 400 |
| Other | 500 |

Returns JSON body with:

```json
{
  "error": "...",
  "message": "...",
  "correlationId": "..."
}
```

---

# 9. Security, Auth & Session

- JWT Bearer Auth  
- TenantId from claim `"http://benefetch.com/tenantId"` or `"tid"`  
- SessionService returns:
  - TenantId  
  - UserId  
  - DisplayName  
  - Roles  

Endpoint:

```
GET /api/session/context
```

---

# 10. Seed Program Overview

Creates:

- Database + containers  
- Tenants, TenantConfig, PayerConfig  
- Practices & nested Locations  
- Payers  
- Patients & CoverageEnrollments  
- Encounters & EligibilityChecks  
- LookupSets  

Seed includes real-world optometry flow examples:

- Dual coverage  
- Medicare + supplemental  
- Pediatric screenings  
- Inactive coverage  
- Multiple-eligibility check encounter  

---

# 11. Differences vs Original Architecture Document

| Topic | Old | Actual |
|-------|------|--------|
| Service Returns | DTOs | **Entities** |
| Config Storage | Separate container | **In tenants container** |
| Payers PK | /tenantId | **/id** |
| Lookups | Tenant override system | MVP = Global only |
| Indexing | Custom rules | Cosmos default |
| Paging | Continuation tokens | Page + TotalCount |

---

# 12. Final Notes

This markdown is the authoritative architecture reference based on real code.  
Use it for diagrams, onboarding docs, refactor decisions, and future planning.

