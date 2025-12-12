Below is the **authoritative, end-to-end flow** for enforcing a **HIPAA-compliant isolation invariant by practice** in your EC architecture. This is written as an **architectural guarantee**, not just “best practices.”

---

# HIPAA Isolation Invariant (Practice-Scoped)

> **Invariant:**
> A user **may never read, write, or infer PHI** for a `PracticeId` they are not explicitly authorized for — regardless of role, UI behavior, or API misuse.

This invariant is enforced **independently** at:

1. **Identity**
2. **Authorization**
3. **API contracts**
4. **Data model**
5. **Cosmos partitioning**
6. **Query shape**
7. **Operational controls**

No single layer is trusted.

---

## 1️⃣ Identity layer (Auth0 / IdP)

### Token claims (non-negotiable)

Every access token contains:

```json
{
  "tenantId": "t-123",
  "userId": "u-456",
  "practiceIds": ["p-1", "p-2"],
  "roles": ["Staff", "Admin"]
}
```

**Security properties:**

* `tenantId` is **immutable**
* `practiceIds` is an **allow-list**, not a selector
* UI-selected practice must exist in `practiceIds`

❗ **Never accept `practiceId` solely from the client without token validation**

---

## 2️⃣ Authorization layer (API)

### Request flow

1. API extracts:

   * `tenantId` from token
   * `practiceId` from route/query/body
2. API enforces:

```csharp
if (!User.PracticeIds.Contains(practiceId))
    throw new ForbiddenException();
```

This happens **before**:

* service logic
* repository calls
* data access

---

## 3️⃣ API contract discipline

### Mandatory rule

> **Every PHI-touching endpoint requires a `practiceId`.**

Examples:

```http
GET /api/patients?practiceId=...
POST /api/encounters?practiceId=...
GET /api/config/payers?practiceId=...
```

🚫 Forbidden:

* implicit practice inference
* “current practice” stored server-side
* default practice fallback

This prevents **confused-deputy** bugs.

---

## 4️⃣ Service layer enforcement (defense-in-depth)

Services **re-validate** practice membership even though controllers already did.

```csharp
EnsurePracticeAccess(user, practiceId);
```

Why?

* protects against future controller mistakes
* supports background jobs
* supports non-HTTP entry points

---

## 5️⃣ Data model guarantees (Cosmos DB)

### Required PHI fields on every entity

```csharp
TenantId   // partition key
PracticeId // isolation boundary
```

**No PHI entity exists without both.**

Examples:

* Patient
* Encounter
* EligibilityCheck
* PayerConfig

🚫 There are **no cross-practice documents**

---

## 6️⃣ Cosmos partitioning strategy

### Partition key = `/tenantId`

Isolation is enforced by:

* **query shape**, not just partitions
* **practiceId as a mandatory filter**

Why not PK = `/practiceId`?

* multi-practice users need cross-practice ops
* batch operations per tenant
* controlled fan-out is safer than duplication

---

## 7️⃣ Query-shape invariant (most critical)

> **Every PHI query includes BOTH filters:**
>
> * `tenantId = @tenantId`
> * `practiceId = @practiceId`

Example:

```sql
SELECT *
FROM c
WHERE c.tenantId = @tenantId
  AND c.practiceId = @practiceId
```

### Repository rule

Repositories **never expose** methods that omit `practiceId` for PHI reads.

This prevents:

* accidental data leakage
* future dev shortcuts
* “admin sees all” anti-patterns

---

## 8️⃣ Write-path enforcement

On every create/update:

```csharp
entity.TenantId   = token.TenantId;
entity.PracticeId = request.PracticeId;
```

Never trust:

* body-supplied tenantId
* UI-selected practice alone

---

## 9️⃣ Background jobs & integrations

Background workers:

* run with **explicit tenantId + practiceId**
* use the **same repository contracts**
* cannot query “all practices” by default

If a job truly needs multi-practice access:

* it loops explicitly
* logs each scope transition

---

## 🔐 HIPAA threat model coverage

| Threat                          | Mitigation                     |
| ------------------------------- | ------------------------------ |
| Broken object-level auth        | Token → practice allow-list    |
| Horizontal privilege escalation | Mandatory practice filter      |
| Confused deputy                 | Practice required on every API |
| Query fan-out leakage           | Repository contracts           |
| Admin overreach                 | Explicit scope switching       |
| Accidental dev error            | Defense-in-depth layers        |

---

## 10️⃣ Audit & compliance posture

This architecture supports:

* **Access logs**: `(userId, tenantId, practiceId, route)`
* **Breach containment**: leakage limited to a single practice
* **Evidence for auditors**:

  * documented invariants
  * enforced at multiple layers
  * no single point of failure

---

## ✅ Final guarantee

> **Even if the UI is compromised, even if a developer makes a mistake, and even if a role is misconfigured — PHI cannot cross practice boundaries.**

This is what HIPAA auditors care about.

If you want, next I can:

* formalize this as a **written HIPAA safeguard policy**
* annotate your existing controllers with **practice isolation comments**
* produce a **“what would violate HIPAA” red-flag checklist for developers**
