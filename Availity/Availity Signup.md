Below is a **clear, implementation-oriented guide** for **signing up with Availity specifically as an API consumer**, not just a portal user. This is written for a **software vendor / clearinghouse integrator** like BF / ECE.

---

# Availity API Signup & Enablement Guide

*(Eligibility, 270/271 focus)*

## 1. Create an Availity Portal Account (Required First Step)

All API access starts with a **standard Availity portal account**.

**Who should do this**

* You (as the vendor / primary admin)
* OR a designated technical admin for BF

**Steps**

1. Go to **Availity.com**
2. Select **Register**
3. Choose **Health Care Organization** (even as a vendor)
4. Complete:

   * Organization name (your company)
   * Tax ID (can be vendor EIN)
   * Primary admin user
5. Verify email + log in

👉 At this point, you have **portal access only** (no APIs yet).

---

## 2. Enroll as an Availity Developer (API Program)

Once logged into the portal:

1. Navigate to **Developer / API** section
   (often under *My Account* → *Developer*)
2. Apply for **Availity API Access**
3. Identify yourself as:

   * **Software Vendor**
   * **Eligibility / Benefits consumer**
4. Provide:

   * Company description
   * Intended use (real-time eligibility checks, 270/271)
   * Estimated transaction volume
   * Environments needed (Sandbox + Production)

⚠️ This step typically triggers **manual review**.

---

## 3. Sign Availity Legal & Compliance Agreements

Availity will require:

* **API Terms of Use**
* **Data Use Agreement (DUA)**
* **BAA-like HIPAA assurances** (since PHI flows)

Expect:

* Legal contact info
* Signed electronic agreements
* Clarification that **you are not redistributing raw payer data**

👉 Approval can take **several business days**.

---

## 4. Obtain API Credentials (OAuth2)

Once approved, Availity provisions:

### You receive:

* **Client ID**
* **Client Secret**
* **OAuth2 token endpoint**
* **Eligibility API base URLs**

Credentials are typically **per environment**:

* Sandbox
* Production

### Auth model

* **OAuth 2.0 Client Credentials Grant**
* Token lifespan is short (minutes)
* Tokens must be cached + refreshed automatically

> ⚠️ Tokens are **organization-scoped**, not user-scoped.

---

## 5. Enable Eligibility / X12 Transactions (270/271)

API access alone is not sufficient — **eligibility must be explicitly enabled**.

Availity will:

* Enable **X12 270/271 eligibility**
* Associate your organization with:

  * Supported payers
  * Allowed service type codes
* Confirm:

  * Test member IDs
  * Payer routing rules
  * Sandbox behavior (some payers stub responses)

You may be asked:

* Which **payer networks** you need
* Whether requests are **patient-initiated or provider-initiated**
* How you handle **subscriber vs dependent logic**

---

## 6. Payer Enrollment (Critical + Often Missed)

Even after API approval, **many payers require separate enrollment**.

Key points:

* Some payers allow **generic eligibility**
* Others require:

  * Provider NPI
  * TIN
  * Contracted relationship
* Availity will tell you:

  * Which payers are auto-enabled
  * Which require manual enrollment

👉 This directly impacts **ECE design**:

* Eligibility failures ≠ system failures
* Must distinguish *“payer not enrolled”* vs *“no coverage”*

---

## 7. Sandbox Testing & Certification

Before production:

* Validate:

  * OAuth token flow
  * 270 submission
  * 271 parsing
* Confirm:

  * AAA rejection handling
  * EB segment interpretation
  * Timeouts + retry behavior

Availity may:

* Require sample transactions
* Ask for test results
* Certify readiness

---

## 8. Production Promotion

After validation:

* Production credentials are activated
* Rate limits are enforced
* SLA expectations apply

Best practice:

* Separate **tenant-level config** per BF tenant
* Never share credentials across tenants
* Store secrets in **Key Vault**

---

## 9. Ongoing Operations Expectations

Availity expects:

* Proper retry handling (timeouts, 503s)
* No credential sharing
* No logging of raw PHI
* Audit-friendly payload retention

They may:

* Monitor transaction volumes
* Throttle abusive traffic
* Require re-certification for major changes

---

## 10. Practical Timeline (Realistic)

| Step               | Typical Time            |
| ------------------ | ----------------------- |
| Portal account     | Same day                |
| Developer approval | 2–7 business days       |
| Legal agreements   | 1–5 business days       |
| API credentials    | 1–2 days after approval |
| Payer enablement   | Variable (days → weeks) |
| Production go-live | 2–4 weeks total         |

---

## What This Means for BF / ECE

Design implications (which you’re already doing correctly):

* **Clearinghouse abstraction** (Availity ≠ only provider)
* **Retryable vs non-retryable failures**
* **Payload storage outside Cosmos**
* **Tenant-specific credentials**
* **Graceful degradation when payer not enabled**

---

If you want next, I can:

* Draft a **checklist you hand to Availity onboarding**
* Provide **exact OAuth2 token request examples**
* Create a **BF internal “Availity readiness” SOP**
* Map **Availity rejection codes → ECE status taxonomy**

Just tell me which deliverable to produce next.
