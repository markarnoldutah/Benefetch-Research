Short answer: **no — “coverage lines” are not something you send to Availity or create during the request.**
They are **derived after the call**, by **parsing Availity’s response**.

Below is the clear mental model.

---

## What “coverage lines” actually are (in BF)

**Coverage lines are a BF abstraction**, not an Availity object.

They are:

* normalized
* human-readable
* UI-friendly summaries

…extracted from Availity’s **271 eligibility/benefits response**.

Think of them as:

> “The handful of facts a front-desk user actually needs to see.”

Examples:

* Vision exam copay = $20
* Eligibility status = Active
* Frequency = 1 exam per 12 months
* Deductible remaining = $0
* Coverage effective date = 01/01/2025

These become your `EligibilityBenefitLineDto[]`.

---

## What happens during the Availity call (important distinction)

### During the **request** (POST /coverages)

You send:

* patient identifiers
* payerId
* provider info
* serviceType codes (e.g., `30`)
* date of service

You **do not** send:

* copays
* deductibles
* benefit descriptions
* coverage lines

At this stage, Availity is just receiving a **270 eligibility inquiry**.

---

### During the **response** (GET /coverages/{id})

Availity returns a **271-style response**, which may include:

* plan status (Active / Inactive)
* effective & termination dates
* benefit segments
* service-type–specific benefits
* messages / notes
* sometimes COB hints

⚠️ The structure is **payer-specific and inconsistent**.

This raw response is **not UI-ready**.

---

## Where coverage lines are created (BF responsibility)

Coverage lines are created **inside BF**, after Availity responds with `statusCode = "4"`.

### BF flow:

1. Poll Availity until `statusCode = "4"`
2. Receive raw response payload
3. Store raw payload (blob)
4. **Parse & normalize**
5. Produce coverage lines
6. Persist them with the EligibilityCheck result
7. Return them to the UI

So:

```
Availity response
      ↓
BF parsing / normalization
      ↓
CoverageLines[]
```

---

## Why this separation is intentional (and correct)

### 1. Availity responses are not consistent

* Different payers use different benefit segments
* Some return copays clearly
* Some return free-text messages
* Some omit fields entirely

You **cannot rely on Availity to give you a neat list**.

---

### 2. Coverage lines are **presentation-layer data**

They exist to:

* drive UI
* support quick decisions
* avoid re-parsing raw payloads every time

They are **derived data**, not source-of-truth data.

---

### 3. Coverage lines can evolve without re-calling Availity

If you improve parsing later:

* you can regenerate coverage lines from stored raw payloads
* no need to re-hit Availity
* no risk of double-charging / rate limits

This is a huge long-term win.

---

## What a coverage line typically maps from

| Coverage line     | Source in Availity                     |
| ----------------- | -------------------------------------- |
| Active / Inactive | plan.status                            |
| Effective date    | plan.effectiveDate                     |
| Termination date  | plan.terminationDate                   |
| Copay             | benefit segments (service-type scoped) |
| Deductible        | benefit segments                       |
| Frequency         | benefit segments or messages           |
| Notes             | payer messages / free text             |

BF decides:

* what to extract
* how confident it is
* what to show vs hide

---

## Important: coverage lines are **not guaranteed**

Sometimes Availity returns:

* “Active” but no benefit detail
* messages like “Contact payer”
* partial data

That’s OK.

Coverage lines should support:

* partial lists
* “unknown” values
* notes instead of numbers

---

## Recommended BF rule of thumb

> **Coverage lines are best-effort summaries, not legal benefit guarantees.**

Always:

* store raw payload
* show disclaimers in UI
* allow staff to view the PDF / raw response

---

## Final one-sentence answer

**Coverage lines are not added during the Availity call — they are created by BF *after* the Availity response arrives, by parsing and normalizing the returned eligibility/benefits data into UI-friendly summary rows.**

If you want next, I can:

* sketch a **simple first-pass coverage line parser** (what to extract on day one),
* recommend which benefit types to ignore initially,
* or map coverage lines to your **encounter + billing workflow** so they stay minimal and safe.
