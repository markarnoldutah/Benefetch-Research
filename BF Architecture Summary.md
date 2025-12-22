# BF (Benefetch) — Architecture Summary

**Status:** Authoritative

This document supersedes all prior BF architecture summaries. It is aligned strictly to:

* The **actual source code** in `API.zip`, `Domain.zip`, `Repositories.zip`, and `cosmos-seed.cs`
* The uploaded **HIPAA Safeguard Policy**, **API Scoping Strategy**, **PHI Entity Classification**, and **HIPAA Isolation Invariant**



---

## 1. Architectural Objective

Benefetch (BF) is a cloud-native, multi-tenant SaaS platform that assists optometry front desks with eligibility, benefits, and coordination-of-benefits (COB) decisions.

The primary non-functional requirement governing all architecture decisions is:

> **HIPAA-compliant isolation of PHI at the practice level.**

---

## 2. HIPAA Isolation Invariant (Non‑Negotiable)

**PracticeId is the PHI isolation boundary.**

PHI belonging to one practice must never be accessible, inferable, or modifiable by:

* Users not explicitly authorized for that practice
* Admin users without explicit practice scope
* Background jobs without explicit practice context
* Misconfigured UI or client requests

This invariant is enforced independently at:

1. Identity (JWT claims)
2. API contracts
3. Controller guards
4. Service-layer validation
5. Repository contracts
6. Cosmos DB query shape

No single layer is trusted.

---

## 3. Identity & Claims Model

### Required JWT Claims

BF access tokens contain:

* `http://benefetch.com/tenantId`
* One or more `http://benefetch.com/practiceId` claims
* `sub` (userId)
* Role claims (non-authoritative for PHI scope)
* Permission claims (non-authoritative for PHI scope)

**Important:**

* Practice claims form an **allow‑list**, not a selector
* UI-selected practice must exist in the claims
* Tokens are immutable for the duration of a request

---

## 4. API Scope Parameter Strategy

**Scope identifiers come from context, not request bodies.**

### Rules

* `tenantId`

  * Always derived from JWT claims
  * Never accepted from request DTOs

* `practiceId`

  * Supplied as a **route parameter** for PHI endpoints
  * Validated against practice claims before any service call
  * Never trusted from request bodies

* Resource identifiers (`patientId`, `encounterId`)

  * Included in route when the resource is the subject

This prevents confused‑deputy and horizontal privilege‑escalation bugs.

---

## 5. PHI Classification (Authoritative)

### PHI Root Entities (Practice‑Scoped)

These entities **must** include both `TenantId` and `PracticeId` and may never be queried without both:

* **Patient**
* **Encounter**

### PHI Embedded Objects (Inherit Scope)

These never exist independently:

* CoverageEnrollment
* EligibilityCheck
* EligibilityPayload (references)
* CoverageDecision
* CoverageLine

### Practice‑Scoped Operational Config

* **PayerConfig**

  * Does not contain clinical data
  * Directly influences PHI interpretation and billing
  * Treated as practice‑scoped

### Explicitly Non‑PHI Entities

These must *not* be treated as PHI and must not gain `PracticeId`:

* Tenant
* TenantConfig
* Practice (organizational only)
* Payer (catalog)
* LookupSet / LookupItem

---

## 6. Solution Structure

### API Project

Responsibilities:

* Authentication & authorization enforcement
* Controller‑level practice guards
* Mapping entities → DTOs
* Exception handling via middleware

Controllers:

* Extract `tenantId` from claims
* Validate `practiceId` against claims
* Call services only after authorization succeeds

### Domain Project

Contains:

* Entities and embedded value objects
* DTOs
* Service and repository interfaces
* Shared utilities

Domain services return **entities**, not DTOs.

### Repositories Project

Responsibilities:

* Cosmos DB access
* Partition key enforcement
* Mandatory practice filtering for PHI
* Cosmos SQL query construction

Repositories **must not expose** PHI methods that omit `practiceId`.

---

## 7. Cosmos DB Data Model

### Partition Strategy

* **Partition Key:** `/tenantId`

Practice isolation is enforced by **query shape**, not partition key choice.

Every PHI query must include:

```sql
WHERE c.tenantId = @tenantId
  AND c.practiceId = @practiceId
```

### Containers

* `tenants`

  * Tenant
  * TenantConfig

* `practices`

  * Practice + Locations

* `patients`

  * Patient + CoverageEnrollments

* `encounters`

  * Encounter + EligibilityChecks + COB

* `payers`

  * GLOBAL payer catalog
  * Practice‑scoped PayerConfig documents

* `lookups`

  * Global lookup sets

GLOBAL/shared catalog data uses `tenantId = "GLOBAL"`.

---

## 8. API Surface (As Implemented)

### Tenant Configuration

* `GET /api/tenants/config`
* `POST /api/tenants/config`
* `PUT /api/tenants/config`
* `GET /api/tenants/access-gate`

### Patients (PHI)

Base route:

```
/api/practices/{practiceId}/patients
```

Includes:

* Create, update, get
* Coverage enrollment CRUD
* Patient‑scoped encounter search

### Encounters (PHI)

Base route:

```
/api/practices/{practiceId}/encounters
```

Includes search and detail endpoints.

### Payers & Payer Config

* `GET /api/payers`
* `GET /api/payers/{payerId}`
* `GET /api/payers/practices/{practiceId}/config`
* `PUT /api/payers/practices/{practiceId}/{payerId}/config`

### Lookups

* `GET /api/lookups`

---

## 9. AccessGate Enforcement

### Policy

TenantConfig includes an **AccessGate** section used to disable access for:

* Billing issues
* Administrative holds
* Security events

### Code Status

* `TenantAccessGateMiddleware` exists
* It is **not currently registered** in `Program.cs`
* Server‑side enforcement is therefore **not active yet**

This must be enabled for full policy compliance.

_TODO - consider enabling the middleware but querying Azure Table Services instead of Cosmos for the gate check to reduce RUs consumed._

---

## 10. Explicit Prohibitions (Developer Red Flags)

The following are considered HIPAA violations in BF:

* Querying PHI without `practiceId`
* Accepting scope identifiers from request bodies
* Tenant‑wide PHI queries
* “Admin can see all practices” logic
* Background jobs without explicit practice scope
* PHI entities without `PracticeId`

---

## 11. Final Architectural Guarantee

> **Even if the UI is compromised, even if a developer makes a mistake, and even if a role is misconfigured — PHI cannot cross practice boundaries in Benefetch.**

This invariant is the foundation of BF’s HIPAA compliance posture.

---

**This document is now the canonical BF Architecture Summary.**
