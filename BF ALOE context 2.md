# BF + ALOE Context (Aligned with Authoritative BF Architecture)

**Status:** Supporting Context (Aligned)

This document provides **product and vision context** for BF (Benefetch) and ALOE (AloeOps) and is explicitly aligned to the **BF Architecture Summary (Authoritative – HIPAA + Code Aligned)**.

Where architectural, security, or data-handling details are concerned, the **BF Architecture Summary is authoritative** and supersedes any implication in this document.

---

## 1. Relationship Between BF and ALOE

BF (Benefetch) and ALOE (AloeOps) are **separate SaaS products** that share:

* A common architectural philosophy
* A multi-tenant, Azure-native implementation style
* A strong emphasis on **security boundaries and isolation invariants**

They **do not share PHI, databases, or runtime scope**.

---

## 2. BF (Benefetch) — Scope Recap

BF is a **front-desk co-pilot** for optometry practices, focused on:

* Eligibility verification
* Benefits interpretation
* Coordination-of-benefits (COB) decisioning

Designed for:
* Optometry
* Ophthalmology
* Dental/Ortho
* Future multi-specialty expansion

### BF Security Posture (Canonical)

* **PracticeId is the PHI isolation boundary**
* PHI is limited to Patient- and Encounter-scoped data
* Access is enforced via:

  * JWT claims
  * Route-based practice scoping
  * Repository-level query guarantees

BF must remain HIPAA-compliant by design.

---

## 3. ALOE (AloeOps) — Product Scope

ALOE (Assisted Living Operating Environment) is an **operational SaaS platform** for Assisted Living and Memory Care operators.

ALOE focuses on:

* Task execution and shift workflows
* Incident reporting
* Family communication
* Operational visibility for administrators

ALOE **does not function as a clinical EMR** and intentionally avoids duplicating PointClickCare (PCC) or similar systems.

#### **High-Level ALOE Module Roadmap**

**Phase 1 (MVP):**

1. Care Tasks (ADLs + custom workflows)
2. Incident Reporting
3. Shift Handoffs
4. Family Communication Portal

**Phase 2:**
5. Activities Tracking
6. Staff Scheduling Lite
7. Maintenance & Work Orders

**Phase 3:**
8. Service Plan Builder
9. Infection Control Dashboard
10. Predictive Staffing Insights

ALOE is designed to scale across **enterprise operators** (e.g., 10–300 buildings) and unify operational standards.

---

## 4. PHI Boundary Clarification Between Products

### BF

* Processes **regulated PHI**
* Enforces practice-level isolation
* Subject to HIPAA safeguards

### ALOE

* Designed to operate with **minimal or no clinical PHI**
* Focuses on operational records rather than medical records
* If PHI is introduced in future ALOE modules, it must:

  * Be explicitly classified
  * Have a clearly defined isolation boundary
  * Follow BF-style invariant enforcement

There is **no implicit PHI-sharing model** between BF and ALOE.

---

## 5. Architectural Alignment (Without Conflation)

Shared principles:

* Cloud-native (.NET + Azure)
* API-first design
* Explicit scope handling
* Defense-in-depth security

Explicit non-goals:

* Shared databases
* Shared authentication scope
* Cross-product data access
* Implicit scope inheritance

BF and ALOE may share **patterns**, but not **runtime trust**.

---

## 6. Naming and Branding Alignment

* **BF** is the canonical abbreviation for Benefetch
* **ALOE** refers to the Assisted Living Operating Environment
* **AloeOps** is the preferred product and domain branding for ALOE

No legacy naming (EC, AL-OS) should appear in forward-facing documentation.

---

## 7. Documentation Authority Hierarchy

1. **BF Architecture Summary (Authoritative – HIPAA + Code Aligned)**
2. HIPAA policy documents
3. BF + ALOE Context (this document)
4. Business summaries and pitch materials

This ordering prevents architectural drift and security ambiguity.

---

## 8. Final Clarification

This document intentionally avoids restating BF architectural mechanics in detail.

Its role is to:

* Clarify **product boundaries**
* Prevent **PHI scope confusion**
* Align vision-level language with **enforced architecture**

All implementation details should defer to the canonical BF Architecture Summary.


## Who am I
**1. Who I Am & My Technical Stack**

You are a **cloud-native software developer** specializing in:

* **.NET, ASP.NET Core**
* **Blazor (WASM + Server)**
* **Azure PaaS**
* **Cosmos DB multi-tenant architecture**

Your architecture preferences include:

* Domain → Repository → Service → Controller layering
* Exception middleware (not per-controller try/catch)
* Partitioning almost always by **tenantId**
* Clean DTO → Entity separation
* API-first design for future extensibility
