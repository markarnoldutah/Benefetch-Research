Below is a **one-page internal reference** you can drop into your repo/wiki. It’s written for engineers working on BF and assumes familiarity with ASP.NET, Polly, and async APIs.

---

# Availity Integration Rules (Internal)

**Applies to:** BF Eligibility (270/271) via Availity
**Audience:** BF backend + frontend engineers
**Goal:** Reliable, user-friendly eligibility checks without blocking UI or overloading Availity

---

## 1) Architectural Rule (Non-Negotiable)

**UI never calls Availity.**
**BF owns all Availity interaction.**

```
UI  →  BF API  →  Availity API
```

BF absorbs:

* async behavior
* polling
* retries & circuit breaking
* error translation
* payload storage

UI only:

* starts a check
* polls BF
* renders status/result

---

## 2) Auth Rule

* OAuth2 Client Credentials required for all Availity calls.
* Tokens are **short-lived (~5 minutes)**.
* Cache tokens; expect frequent refresh.
* Treat `401 invalid_token` as normal → refresh and retry once.

---

## 3) Canonical Eligibility API

* **Submit:** `POST /availity/v1/coverages`

  * `Content-Type: application/x-www-form-urlencoded`
* **Poll:** `GET /availity/v1/coverages/{id}`

❌ Do not use legacy `GET /coverages` submit behavior (deprecated).
✅ Always store and poll by returned `id`.

---

## 4) Async Status Model (Critical)

**HTTP status does NOT indicate business completion.**
Always inspect `statusCode` in the payload.

### Availity → BF status mapping

| Availity `statusCode` | Meaning                  | BF Status                               |
| --------------------- | ------------------------ | --------------------------------------- |
| `"0"`                 | In Progress              | `InProgress`                            |
| `"R1"`                | Retrying (temporary)     | `InProgress`                            |
| `"4"`                 | Complete                 | `Complete`                              |
| `"19"`                | Request/validation error | `Failed (RequestValidation)`            |
| `"7"`                 | Communication error      | `InProgress` → `Failed` (policy-driven) |

Only `"4"` is a success terminal state.

---

## 5) Polling Rules

* **One Availity GET per UI poll** (max).
* UI polls BF every **1–3 seconds**.
* BF must return fast (no long blocking).
* Stop polling when BF status is `Complete` or `Failed`.

**Never loop internally** waiting for Availity to complete.

---

## 6) Retry & Circuit Breaker (Polly)

Apply Polly **only** around Availity calls.

### POST `/coverages`

* Retry on transient network failures (small count).
* If circuit breaker is open:

  * return BF `InProgress` with “temporarily delayed” messaging.

### GET `/coverages/{id}`

* Light retry (0–1).
* If breaker open:

  * return BF `InProgress`, not an exception.

**Rationale:** Availity delays are normal; BF must stay responsive.

---

## 7) Error Translation Rules

Never surface raw Availity errors to end users.

### Translate to BF concepts:

* **Validation (19):** “Missing or invalid information” → show fixable fields.
* **Retrying (R1):** “Payer is slow—still checking.”
* **Comm error (7):** “Eligibility temporarily unavailable—try again.”

Log raw Availity payloads for diagnostics; expose friendly messages only.

---

## 8) Data Persistence Rules

* Store **EligibilityCheck** under the Encounter (audit trail).
* Persist:

  * `eligibilityCheckId`
  * `availityCoverageId`
  * BF status + last `remoteStatusCode`
  * timestamps & attempt counts
* Store **large payloads** (PDF, raw JSON) in **blob storage**.
* Keep only **URLs + metadata** in Cosmos.

---

## 9) Idempotency Rule

Front desk users will double-click.

* Kickoff endpoint **must be idempotent**.
* If an equivalent check is already `InProgress` or `Complete`:

  * return the existing `eligibilityCheckId`.

---

## 10) Payer Variability Rule

* Payers differ in:

  * required fields
  * speed
  * enrollment requirements
* Always rely on:

  * **Payer List API** (who supports 270 via API)
  * **Configurations API** (required fields & validation)
* Never hardcode payer assumptions.

---

## 11) Mocking & Testing Rule

Mocks must simulate reality:

* async submit → poll
* `0 → 4`, `R1 → 4`, `19`, `7`
* delays and retries
* scenario selection via headers (e.g., `X-Api-Mock-Scenario-ID`)

Mocks that always return “success” are unacceptable.

---

## 12) UX Contract

* UI shows: *Checking → Complete / Failed*
* UI never blocks on a single request.
* After ~30–45s, allow “still processing—refresh later.”

---

## Bottom Line

> **Availity is async, payer-variable, and occasionally slow.
> BF must be resilient, non-blocking, and user-friendly at all times.**

Follow these rules and the integration will behave correctly in sandbox, production, peak hours, and outages—without surprises.
