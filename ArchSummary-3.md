# BF (Benefetch) – Updated Architecture Summary
Updated to match the current implementation in **API.zip**, **Domain.zip**, **Repositories.zip**, and **seed-program.cs**.  
All conflicts with the older "ChatGPT Architecture Summary" are resolved in favor of the real code.

## Seed Program Source of Truth

**cosmos-seed.cs** is the authoritative Cosmos DB seeding program (superseding any earlier `seed-program.cs` where conflicts exist).

---

# 1. High-Level Architecture & Goals

The BF system is a cloud-native, multi-tenant, optometry-focused API designed as a **front-desk co-pilot** for insurance eligibility & benefits verification.

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
| **payers** | `/tenantId` | Payer master data (GLOBAL + per-tenant) |
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
- Benefits-check retrieval  
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

### Partitioning note:

`payers` container uses **PK = /tenantId**. Global/shared payers are stored under the shared partition value `tenantId = "GLOBAL"`, while tenant-specific payers live under their tenantId partition.

Repositories and queries should consistently supply the correct `tenantId` partition key (including `"GLOBAL"` where appropriate) to avoid cross-partition scans.

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
| Payers PK | /id | **/tenantId** (GLOBAL shared partition) |
| Lookups | Tenant override system | MVP = Global only |
| Indexing | Custom rules | Cosmos default |
| Paging | Continuation tokens | Page + TotalCount |

---

# 12. Final Notes

This markdown is the authoritative architecture reference based on real code.  
Use it for diagrams, onboarding docs, refactor decisions, and future planning.


## Cosmos DB Containers

The seed program creates/uses the following containers (all partitioned by **`/tenantId`**):

- **tenants**: `Tenant` + `TenantConfig` documents (distinguished by `Type`)
- **practices**: `Practice` documents (with embedded `Locations`)
- **patients**: `Patient` documents (with embedded `CoverageEnrollments`)
- **encounters**: `Encounter` documents (with embedded eligibility checks, payload refs, and COB decisions)
- **payers**: `Payer` + `PayerConfig` documents (distinguished by `Type`)
  - Global/shared payers live in the `tenantId = \"GLOBAL\"` partition; tenant-specific payers live in their tenant partition.
- **lookups**: `LookupSet` documents

Notes:
- **TenantConfig** is the frontend bootstrap source-of-truth.
- **PayerConfig** is **practice-only** at runtime (each document includes a required `PracticeId`).
- Tenant-wide consistency is handled via a **tenant-level payer template** (stored in the `tenants` container as `type = "payerTemplate"`) that is **materialized** into practice payer configs.


## Frontend Bootstrap Flow (Blazor WASM)

The Blazor frontend boots without calling `/api/tenants/{tenantId}`. Instead, it initializes entirely from **TenantConfig** and supporting reference sets.

Startup sequence:

1. **Authenticate** and acquire an access token that includes `tenantId`.
2. **GET `/api/tenantConfig`** (tenant inferred from token)
   - Includes **AccessGate** (see below) and startup-critical settings (practice defaults, encounter types, eligibility behaviors, COB rules, UI settings).
3. **Gate check**: if `tenantConfig.AccessGate.LoginsEnabled == false`
   - Stop bootstrapping and show `DisabledMessage` (plus `SupportContactEmail`).
4. **GET `/api/config/payers?practiceId=...`**
   - Load **practice-only** payer configs for the active practice.
   - No tenant-default fallback or override resolution at runtime.
5. **GET `/api/lookups`** `/api/lookups`**
   - Load lookup sets used for dropdowns, labels, filters, and consistent UI rendering.
6. **Select active practice**
   - If multiple practices exist, prompt the user; otherwise auto-select.
   - The active `practiceId` drives which **practice payer configs** are loaded and used for encounters.



## Payer Template and Practice Payer Config Workflow

Key decision: **eliminate tenant-default payer configs at runtime**.

### Data model

- **GLOBAL payer catalog**: stored in the `payers` container with partition key `/tenantId` and shared partition value `"GLOBAL"`.
- **Tenant payer template**: stored in the `tenants` container (type discriminator `payerTemplate`). Used for onboarding and consistency.
- **Practice payer configs**: stored in the `payerConfigs` container (PK `/tenantId`). Each document includes `PracticeId` and `PayerId`.

### Template application

- **On practice creation**: apply tenant payer template → materialize `PayerConfig` docs for the practice.
- **Admin operations**: apply template to one practice or all practices.
- Implementation uses **TransactionalBatch** in Cosmos (same `/tenantId` partition) for efficient bulk upserts.

### API endpoints (verbatim)

- Bootstrap
  - `GET /api/config/tenant`

- Practice payers
  - `GET /api/config/payers?practiceId=...`
  - `POST /api/config/payers/apply-template?practiceId=...`
  - `POST /api/config/payers/apply-template/all`

- Template admin
  - `GET /api/config/payer-template`
  - `PUT /api/config/payer-template`


### TenantConfig Access Gate

TenantConfig includes an `AccessGate` section used for startup gating and server enforcement:

- `LoginsEnabled` (bool)
- `DisabledReason` (e.g., PastDue, Suspended, Canceled, ManualHold, SecurityLock)
- `DisabledMessage` (safe, user-facing)
- `SupportContactEmail`
- `DisabledAtUtc`

This enables consistent UX messaging while ensuring the server can deny access for non-payment or administrative hold.


## Practice Payer Config Model (Runtime)

Payer configuration at runtime is **practice-only**.

- Stored as `PayerConfig` documents in the **`payerConfigs`** container
- Cosmos partition key: **`/tenantId`**
- Each document includes a required **`PracticeId`** and **`PayerId`**

Runtime query shape:

```sql
SELECT *
FROM payerConfigs c
WHERE c.tenantId = @tenantId
  AND c.practiceId = @practiceId
ORDER BY c.sortOrder
```

There is **no tenant-default fallback** and no inheritance logic at runtime. Tenant-wide consistency is achieved via the **tenant payer template** (see “Payer Template and Practice Payer Config Workflow”).

## Tenant AccessGate Enforcement (API)

In addition to frontend gating, the API should enforce tenant access centrally (recommended: middleware).

- Allowlist: `/api/tenantConfig` (and optionally `/health`, `/swagger`)
- For all other routes:
  - Read `tenantId` from token claims
  - Load TenantConfig (cached) and deny requests when `AccessGate.LoginsEnabled == false`
  - Return `403` with `{ error: "TenantDisabled", reason, message, support }`

This prevents bypassing UI gating and ensures consistent behavior across all endpoints.
