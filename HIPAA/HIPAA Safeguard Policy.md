Absolutely — below is the **same three deliverables**, rewritten to refer consistently to **BF (Benefetch)** instead of EC.
Nothing else has changed semantically; this is a **pure naming + product-context update** aligned with your current BF architecture and Cosmos model.

---

# 1️⃣ BF HIPAA Technical Safeguard Policy

*(Auditor-ready, BAA-appropriate language)*

## Policy: Practice-Level PHI Isolation in Benefetch (BF)

**Purpose**
To ensure Protected Health Information (PHI) processed by **Benefetch (BF)** is accessed, stored, and transmitted in strict compliance with HIPAA by enforcing **practice-level isolation** across all BF systems.

---

### 1. Scope

This policy applies to:

* All Benefetch APIs
* All BF data stores (Cosmos DB)
* All background jobs and integrations
* All BF users, roles, and administrative accounts

---

### 2. Isolation Invariant

> PHI belonging to one **PracticeId** must never be accessible to any user or process not explicitly authorized for that PracticeId.

This invariant holds regardless of:

* user role (including admins)
* UI state
* client-side behavior
* internal service or integration context

---

### 3. Identity & Authentication Controls

* All BF access tokens include:

  * `tenantId`
  * `practiceIds[]` (explicit allow-list)
* Tokens are validated on every request.
* Practice access decisions are derived **only** from token claims.

---

### 4. Authorization Controls

* Every PHI-related BF API requires an explicit `practiceId`.
* The API validates that the requested `practiceId` exists in the caller’s token claims.
* Requests failing validation return **HTTP 403 Forbidden**.

---

### 5. Data Model Controls

* All PHI entities in BF include:

  * `TenantId`
  * `PracticeId`
* No PHI entity exists without both fields populated.
* PHI is never shared across practices.

---

### 6. Data Access Controls

* All Cosmos DB queries accessing PHI must include:

  * `tenantId = @tenantId`
  * `practiceId = @practiceId`
* Repository interfaces prohibit PHI queries that omit `practiceId`.

---

### 7. Operational Controls

* Background jobs must run with explicit `(tenantId, practiceId)` scope.
* Multi-practice operations are executed as explicit loops with audit logging.
* No implicit “all practices” execution is allowed.

---

### 8. Auditability

Benefetch logs:

* userId
* tenantId
* practiceId
* API route
* timestamp

These logs support HIPAA audits, access reviews, and breach investigations.

---

### 9. Breach Containment

If a defect or compromise occurs:

* Impact is limited to a **single practice**
* Cross-practice PHI exposure is prevented by design

---

# 2️⃣ BF Controller & Repository Annotation Guide

*(Inline documentation for Benefetch developers)*

These annotations exist to **prevent accidental HIPAA violations** during BF development.

---

## Controller example (PHI endpoint)

```csharp
// BF HIPAA ISOLATION RULE:
// - practiceId is REQUIRED
// - practiceId must exist in the authenticated user's token claims
// - No PHI may be accessed without explicit practice scoping
[HttpGet("patients")]
public async Task<IActionResult> GetPatients(
    [FromQuery] string practiceId)
{
    var tenantId = User.GetTenantId();

    // Enforce practice isolation before any service calls
    if (!User.PracticeIds.Contains(practiceId))
        return Forbid();

    return Ok(await _patientService.GetPatientsAsync(
        tenantId,
        practiceId));
}
```

---

## Service-layer guard (defense-in-depth)

```csharp
// Defense-in-depth: services re-validate practice access
EnsurePracticeAccess(user, practiceId);
```

---

## Repository contract (PHI-safe)

```csharp
// BF HIPAA REQUIREMENT:
// All PHI queries MUST include tenantId AND practiceId.
// Repository methods that omit practiceId are prohibited.
Task<IReadOnlyList<Patient>> GetPatientsAsync(
    string tenantId,
    string practiceId);
```

---

## Cosmos query comment

```sql
-- BF HIPAA REQUIREMENT:
-- tenantId + practiceId filters are mandatory to prevent cross-practice PHI leakage
SELECT *
FROM c
WHERE c.tenantId = @tenantId
  AND c.practiceId = @practiceId
```

---

# 3️⃣ BF Developer Red-Flag Checklist

*(“How not to break HIPAA in Benefetch”)*

## 🚨 Absolute Red Flags (Never Allowed in BF)

* ❌ Querying PHI without `practiceId`
* ❌ Accepting `practiceId` without validating against token claims
* ❌ Using server-side “current practice” state
* ❌ Tenant-wide PHI queries
* ❌ Admin bypass logic (“admins can see all practices”)
* ❌ `PracticeId = null` on PHI entities
* ❌ Background jobs without explicit practice scope

---

## ⚠️ High-Risk Changes (Require Review)

* Adding new PHI entities
* Adding bulk exports
* Adding reporting endpoints
* Adding background workers or integrations
* Adding cross-practice admin tooling

Each requires:

* explicit practice scoping
* audit logging
* architectural review

---

## ✅ Safe Patterns (Preferred in BF)

* Explicit `practiceId` on every PHI request
* Token-based practice allow-lists
* Repository methods that *require* `practiceId`
* Explicit loops for multi-practice jobs
* TransactionalBatch operations scoped to one practice

---

## 🧠 BF Developer Mental Model

> “If I remove `practiceId`, I have created a HIPAA violation in Benefetch.”

If a feature is easier without practice scoping — **it is wrong**.

---

## ✅ Final BF Guarantee

> **Even if the BF UI is compromised, even if a developer makes a mistake, and even if a role is misconfigured — PHI cannot cross practice boundaries.**

This is the core HIPAA invariant enforced by Benefetch.

---

If you want next, I can:

* Turn this into a **formal BF HIPAA Safeguards appendix (PDF / Markdown)**
* Add **unit-test templates** that assert practice isolation
* Produce a **BF auditor-facing architecture diagram**
* Draft a **developer onboarding page** (“How BF enforces HIPAA by design”)

Just say the word.
