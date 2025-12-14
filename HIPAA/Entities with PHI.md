Below is the **authoritative PHI classification for BF**, based on your **Domain.zip entities, seed data, and HIPAA intent**. This is the list you should treat as **non-negotiable for practice isolation**.

---

# BF — Entities That Contain PHI

## ✅ **Primary PHI-Bearing Root Entities**

These **must** be practice-scoped, require `PracticeId`, and must never be queried without it.

### 1️⃣ **Patient**

**PHI level: HIGH**

Contains:

* Name
* Date of birth
* Email / phone
* Coverage enrollments
* Subscriber details
* Relationship to subscriber

HIPAA impact:

* Directly identifiable
* Central PHI record

**Status:**
✔ Correctly includes `TenantId` + `PracticeId`

---

### 2️⃣ **Encounter**

**PHI level: HIGH**

Contains:

* Visit dates
* Visit type
* Patient linkage
* Coverage decisions
* Eligibility results
* Medical vs routine context

HIPAA impact:

* Treatment + billing data
* Links patient, provider activity, and coverage

**Status:**
✔ Correctly includes `TenantId` + `PracticeId`

---

## ✅ **PHI Embedded Value Objects**

These **inherit PHI scope from their parent** (Patient or Encounter).
They do **not** need their own `PracticeId`, but **must never exist independently**.

### 3️⃣ **CoverageEnrollmentEmbedded**

Contains:

* Member ID
* Group number
* Subscriber name & DOB
* Employer plan info

HIPAA impact:

* Insurance identifiers = PHI

---

### 4️⃣ **EligibilityCheckEmbedded**

Contains:

* Eligibility status
* Coverage snapshots
* Member ID snapshots
* Raw payer responses
* Error messages

HIPAA impact:

* Insurance + treatment eligibility
* Time-bound PHI

---

### 5️⃣ **EligibilityPayloadEmbedded**

Contains:

* Pointers to X12 270/271 payloads
* Request/response metadata

HIPAA impact:

* Payloads reference PHI even if stored externally

---

### 6️⃣ **CoverageDecisionEmbedded**

Contains:

* Which plan was used
* COB rationale
* Override notes (user-entered)

HIPAA impact:

* Billing + clinical decision context

---

### 7️⃣ **CoverageLineEmbedded**

Contains:

* Copays
* Deductibles
* Allowances
* Service coverage details

HIPAA impact:

* Financial + treatment-related data

---

## ⚠️ **Conditional / Operational PHI**

These entities **do not contain clinical data**, but still require practice isolation because they influence PHI access or billing behavior.

### 8️⃣ **PayerConfig** (practice-only in new model)

Contains:

* Which payers are enabled
* COB defaults
* Display rules

HIPAA relevance:

* Determines how PHI is interpreted and billed

**Status:**
⚠️ Should require **non-nullable `PracticeId`** going forward (per your new architecture)

---

## ❌ **Non-PHI Entities (Do NOT require PracticeId)**

These must **never** be treated as PHI and should remain tenant- or global-scoped only.

### Tenant / Structure

* `Tenant`
* `TenantConfig`
* `Practice` *(organizational, not patient-specific)*

### Reference / Catalog

* `Payer` (GLOBAL payer master)
* `LookupSet`
* `LookupItem`

These may **reference behavior that affects PHI**, but do not contain PHI themselves.

---

## 🧠 Summary Table

| Entity                     | Contains PHI | Requires PracticeId  |
| -------------------------- | ------------ | -------------------- |
| Patient                    | ✅ Yes        | ✅ Yes                |
| Encounter                  | ✅ Yes        | ✅ Yes                |
| CoverageEnrollmentEmbedded | ✅ Yes        | Inherited            |
| EligibilityCheckEmbedded   | ✅ Yes        | Inherited            |
| EligibilityPayloadEmbedded | ✅ Yes        | Inherited            |
| CoverageDecisionEmbedded   | ✅ Yes        | Inherited            |
| CoverageLineEmbedded       | ✅ Yes        | Inherited            |
| PayerConfig                | ⚠️ Indirect  | ✅ Yes (now required) |
| Practice                   | ❌ No         | ❌ No                 |
| Tenant / TenantConfig      | ❌ No         | ❌ No                 |
| Payer                      | ❌ No         | ❌ No                 |
| Lookups                    | ❌ No         | ❌ No                 |

---

## 🔐 Architectural Rule (lock this in)

> **Any entity that can identify a patient, their coverage, or their treatment context is PHI and must be practice-scoped.**

In BF terms:

* **PHI roots → inherit `PracticeScopedEntityBase`**
* **PHI embeds → never standalone**
* **Configs → practice-scoped if they influence PHI interpretation**

If you want, next I can:

* Produce a **compile-time rule** list (“PHI entities must inherit X”)
* Give you a **one-page HIPAA entity map diagram**
* Draft a **dev checklist** for adding new entities without violating HIPAA
