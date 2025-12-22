Below is a **BF internal Standard Operating Procedure (SOP)** you can hand to engineering, ops, or compliance as the **authoritative readiness checklist** for integrating with **Availity APIs (Eligibility / 270–271)**.

This is written to align with:

* BF / ECE architecture decisions you’ve already made
* Practice-level PHI boundaries
* Multi-tenant, multi-payer reality
* Audit-safe, production-ready operations

---

# BF Internal SOP

## Availity Readiness & Go-Live

**Document Purpose**
Ensure BF is technically, operationally, and contractually ready to use Availity APIs for real-time eligibility checks in ECE.

**Applies To**

* Eligibility Check Engine (ECE)
* Clearinghouse integrations
* Tenant onboarding
* Practice configuration
* Compliance & support

---

## 1. Ownership & Roles

| Role             | Responsibility                       |
| ---------------- | ------------------------------------ |
| Technical Owner  | API integration, resilience, logging |
| Compliance Owner | Agreements, HIPAA alignment          |
| Ops Owner        | Payer enablement, onboarding         |
| Support Owner    | Failure interpretation & escalation  |

No single role may bypass this SOP.

---

## 2. Prerequisites (Must Be Complete Before Coding)

### 2.1 Availity Account & Developer Access

* [ ] Availity portal account created
* [ ] Registered as **Software Vendor**
* [ ] Approved for **API / Developer Program**
* [ ] Eligibility (270/271) explicitly enabled

### 2.2 Legal & Compliance

* [ ] Availity API Terms signed
* [ ] Data Use Agreement signed
* [ ] HIPAA/BAA assurances confirmed
* [ ] Internal compliance sign-off recorded

⚠️ No PHI may be transmitted prior to legal approval.

---

## 3. Credential & Security Setup

### 3.1 OAuth2 Credentials

For **each environment** (Sandbox, Production):

* [ ] Client ID issued
* [ ] Client Secret issued
* [ ] Token endpoint documented
* [ ] Eligibility endpoint documented

### 3.2 Secret Handling

* [ ] Secrets stored in **Key Vault**
* [ ] Never committed to source
* [ ] Never logged
* [ ] Rotatable without redeploy

### 3.3 Token Management

* [ ] OAuth2 Client Credentials grant implemented
* [ ] Token cached in memory
* [ ] Expiry handled with safety window
* [ ] Token refresh thread-safe

---

## 4. Practice & Provider Data Requirements

### 4.1 Mandatory Practice Fields

ECE **must** have access to:

* [ ] PracticeId (PHI boundary)
* [ ] Billing NPI
* [ ] Rendering NPI (if distinct)
* [ ] TIN

These values:

* Are **never supplied by clients**
* Are resolved internally by PracticeId
* Are passed implicitly to clearinghouse adapters

### 4.2 Payer Capability Classification

Each payer must be flagged as:

* [ ] Generic eligibility allowed
* [ ] Provider-specific eligibility required

This determines:

* Required loops in X12 270
* Expected failure modes
* UX messaging

---

## 5. Payer Enablement Checklist

For **each payer** intended for use:

* [ ] Confirm payer supported by Availity
* [ ] Confirm eligibility enabled for BF org
* [ ] Determine if provider enrollment required
* [ ] Document enrollment status:

  * Generic
  * Contracted required
  * Not supported
* [ ] Test with known member data (sandbox or live)

⚠️ “No response” ≠ “No coverage”

---

## 6. Technical Readiness (ECE + Adapter)

### 6.1 Request Construction

* [ ] X12 270 built via translator (not inline logic)
* [ ] CorrelationId propagated end-to-end
* [ ] Date of service explicit
* [ ] Service type codes configurable

### 6.2 Payload Handling

* [ ] Raw 270 stored in Blob
* [ ] Raw 271 stored in Blob
* [ ] Only Blob URLs stored in Cosmos
* [ ] Payload paths excluded from indexing

### 6.3 Resilience

* [ ] HttpClientFactory used
* [ ] Timeout configurable per tenant
* [ ] Retry on:

  * Timeouts
  * 429
  * 503
* [ ] No retry on validation errors
* [ ] Circuit breaker (optional but recommended)

---

## 7. Eligibility Result Classification (Critical)

ECE **must distinguish**:

| Category              | Meaning                         |
| --------------------- | ------------------------------- |
| ActiveCoverage        | Patient insured                 |
| InactiveCoverage      | Coverage ended                  |
| ProviderNotAuthorized | Practice not allowed to inquire |
| NotContracted         | Practice out of network         |
| PayerNotEnabled       | Enrollment missing              |
| Timeout               | Retryable                       |
| SystemError           | Non-retryable                   |

❌ Never collapse these into “no coverage”.

---

## 8. Logging & Observability

### 8.1 Allowed Logs

* CorrelationId
* PayerId
* PracticeId
* High-level status
* Timing metrics

### 8.2 Forbidden Logs

* Member ID
* DOB
* Raw X12
* Token values

### 8.3 Metrics

* Success rate by payer
* Timeout rate
* Retry rate
* Avg response time

---

## 9. Sandbox Validation Checklist

Before production:

* [ ] OAuth token flow validated
* [ ] 270 submission accepted
* [ ] 271 parsed correctly
* [ ] AAA rejections handled
* [ ] Provider-specific failures classified correctly
* [ ] Payloads persisted + retrievable
* [ ] Cancellation tokens respected

---

## 10. Production Go-Live Gate

All must be true:

* [ ] Legal approval complete
* [ ] Credentials active
* [ ] At least one payer validated live
* [ ] Support escalation path defined
* [ ] SOP stored in internal docs
* [ ] Rollback plan documented

Only the Technical Owner may approve go-live.

---

## 11. Ongoing Operations

### 11.1 Tenant Onboarding

* Confirm payer coverage expectations
* Verify practice NPI/TIN
* Explain payer-specific limitations

### 11.2 Incident Response

If eligibility failures spike:

1. Check Availity status
2. Check payer enrollment
3. Check credential validity
4. Check timeout metrics
5. Escalate to Availity support if systemic

---

## 12. Non-Negotiable Rules

* Practice is the PHI boundary
* Eligibility ≠ coverage ≠ payment
* Payer behavior is not uniform
* Clearinghouse abstraction must remain intact
* Payloads never live in Cosmos

---

## 13. SOP Review Cadence

* Reviewed quarterly
* Updated when:

  * New payer added
  * Availity API changes
  * ECE eligibility taxonomy changes

---

### ✅ Outcome

Following this SOP ensures:

* Correct eligibility interpretation
* No false “no insurance” results
* Compliance safety
* Production-grade resilience
* Zero architectural rework later

---

If you want next, I can:

* Convert this into a **one-page checklist**
* Create a **tenant onboarding runbook**
* Draft **support scripts** for front-desk staff
* Produce a **payers × requirements matrix** template

Just tell me which one you want.
