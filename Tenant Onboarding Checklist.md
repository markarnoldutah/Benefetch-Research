Below is a **BF internal Tenant Onboarding Runbook** designed to be used by **Ops + Support + Engineering** when bringing a **new tenant** live on **Eligibility Check Engine (ECE)** with **Availity** (and future clearinghouses).

This is **procedural**, step-by-step, and assumes the **Availity Readiness SOP is already approved**.

---

# BF Internal Runbook

## Tenant Onboarding – Eligibility Check Engine (ECE)

**Purpose**
Provide a repeatable, low-risk process to onboard a new tenant for eligibility checks while preventing false “no coverage” results, payer misconfiguration, or compliance issues.

**Audience**

* Ops
* Support
* Engineering (as needed)
* Customer Success

---

## 1. Intake & Qualification (Before Any Setup)

### 1.1 Tenant Discovery Call (Required)

Confirm and document:

* Tenant legal name
* Primary contact (ops + technical)
* Practice count
* Practice specialties (optometry, medical, etc.)
* Expected payer mix (top 5–10)
* Go-live urgency (soft launch vs production critical)

⚠️ Do **not** promise full payer coverage.

---

### 1.2 Eligibility Suitability Check

Verify tenant understands:

* Eligibility ≠ payment guarantee
* Some payers are:

  * Generic eligibility
  * Provider-specific
  * Not supported
* Some failures are **authorization**, not coverage

If tenant expectations are misaligned → **pause onboarding**.

---

## 2. Tenant Creation (System Setup)

### 2.1 Create Tenant Record

* Generate TenantId
* Assign:

  * Name
  * Status = `Provisioning`
* No practices yet

---

### 2.2 Configure Tenant Eligibility Settings

Set defaults (modifiable later):

* Eligibility enabled: `true`
* Primary clearinghouse: `Availity`
* Request timeout (seconds)
* Retry/backoff policy
* Logging level (default: safe)

⚠️ Credentials are **not** added yet.

---

## 3. Practice Onboarding (Critical PHI Boundary)

Repeat **per practice**.

### 3.1 Practice Intake Data (Required)

Collect and verify:

* Practice legal name
* Billing NPI
* Rendering NPI(s) (if applicable)
* TIN
* Physical location (optional but recommended)

Validation rules:

* NPI format valid
* TIN present
* No shared NPIs across unrelated practices

❌ Do not accept this data from patient-facing UI.

---

### 3.2 Create Practice Record

* Assign PracticeId
* Link to TenantId
* Store NPIs + TIN securely
* Mark eligibility status = `Pending`

---

## 4. Clearinghouse Credential Setup (Tenant-Level)

### 4.1 Availity Credentials

For the tenant:

* Client ID
* Client Secret
* Environment (Sandbox / Production)

Store:

* Secure vault only
* Reference by tenant
* Never per practice

---

### 4.2 Credential Validation

Perform:

* OAuth token request
* Token expiry verification
* Connectivity check

If token fails → **stop onboarding**.

---

## 5. Payer Configuration (Where Most Issues Occur)

### 5.1 Payer Selection

From tenant intake:

* Identify target payers
* Map to BF payer catalog

Exclude:

* Unsupported payers
* Payers requiring enrollment tenant cannot complete

---

### 5.2 Payer Capability Classification

For each payer:

Mark:

* Generic eligibility allowed → Yes/No
* Provider-specific eligibility → Yes/No
* Provider enrollment required → Yes/No

Store this classification centrally.

---

### 5.3 Provider Enrollment Verification

If required:

* Confirm practice NPI/TIN enrolled with payer
* Document enrollment status:

  * Complete
  * Pending
  * Not enrolled

⚠️ Pending enrollment = degraded eligibility.

---

## 6. Sandbox Validation (Per Practice)

### 6.1 Test Matrix

Run eligibility checks for:

| Scenario                | Expected                 |
| ----------------------- | ------------------------ |
| Active member           | Coverage returned        |
| Inactive member         | Inactive response        |
| Provider not authorized | Proper classification    |
| Payer not enabled       | Correct failure category |

---

### 6.2 Validation Rules

* No “no coverage” results caused by provider auth issues
* Payloads stored to Blob
* Correct classification surfaced to ECE

Failures must be fixed before proceeding.

---

## 7. Production Enablement

### 7.1 Practice Eligibility Activation

For each practice:

* Set eligibility status = `Active`
* Confirm NPIs/TIN included in requests

---

### 7.2 Limited Live Testing

Perform:

* 1–3 live eligibility checks
* Known insured members
* Observe response classification

If failures:

* Pause
* Investigate payer or enrollment issues

---

## 8. Tenant Go-Live

### 8.1 Status Change

* Tenant status → `Active`
* Practices → `Active`

---

### 8.2 Customer Handoff

Provide tenant with:

* Supported payers list
* Known limitations
* Explanation of eligibility result types
* Support escalation path

---

## 9. Support Playbook (Post Go-Live)

### 9.1 Common Issues & Actions

| Issue                     | Action                 |
| ------------------------- | ---------------------- |
| “No eligibility found”    | Check payer enrollment |
| “Provider not authorized” | Verify NPI/TIN         |
| Timeouts                  | Check Availity status  |
| Inconsistent results      | Verify practice scope  |

---

### 9.2 When to Escalate to Engineering

* Repeated timeouts across tenants
* Incorrect classification
* Token failures
* Payload persistence issues

---

## 10. Rollback Plan

If severe issues:

* Disable eligibility per practice
* Preserve data
* Communicate limitations to tenant
* Resume once resolved

---

## 11. Onboarding Completion Checklist

Before closing onboarding:

* [ ] Tenant active
* [ ] At least one payer validated live
* [ ] Practice data verified
* [ ] Support briefed
* [ ] Documentation delivered
* [ ] Runbook followed fully

---

## 12. Key Principles (Non-Negotiable)

* Practice = PHI boundary
* Eligibility is payer + provider dependent
* Enrollment issues are not system failures
* Accuracy > speed during onboarding

---

### ✅ Outcome

Following this runbook ensures:

* Predictable tenant launches
* No false denial messaging
* Fewer support escalations
* Long-term scalability for multi-payer eligibility

---

If you want next, I can:

* Create a **front-desk UX messaging guide**
* Produce a **payer enrollment questionnaire**
* Generate a **one-page onboarding checklist**
* Build a **support troubleshooting decision tree**

Just tell me which one to do next.
