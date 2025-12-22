Below is a **concrete, front-desk–ready example** of what **ECE should return and display** after an eligibility check.
This is written exactly how a **non-technical staff member** would read it—**clear, cautious, and actionable**—while still being backed by your full classification logic under the hood.

---

# Eligibility Check Result

*(Real-Time Verification)*

---

## 🧍 Patient

**Name:** Jane Doe
**DOB:** 03/14/1986
**Member ID:** ABC123456
**Payer:** Blue Cross Blue Shield
**Plan:** PPO

---

## ✅ Coverage Status

**Coverage appears ACTIVE**
**Effective Date:** 01/01/2025
**As of:** Today’s date

---

## 🏥 Provider Status (Important)

**Your practice is recognized by this payer.**
Eligibility was returned **for your practice**.

---

## 👁️‍🗨️ Benefits at a Glance

*(Information shown is payer-reported and may be incomplete)*

* **Office Visit:** Covered
* **Vision Exam:** Covered
* **Copay:** $25
* **Deductible:** $1,000 (individual)
* **Deductible Remaining:** $400
* **Out-of-Pocket Max Remaining:** $1,200

---

## ⚠️ Important Notes for Front Desk

* Eligibility confirms **active coverage**, not payment guarantee
* Benefits may vary by service or diagnosis
* Prior authorization **may** be required for some services
* Final responsibility determined by the payer

---

## 🕒 Verification Details

* **Checked:** 09:42 AM
* **Source:** Payer via clearinghouse
* **Reference #:** ECE-2025-000483

---

### ✅ Recommended Next Step

Proceed with scheduling and standard intake.

---

---

# Alternate Examples (Critical Scenarios)

Below are **equally important MVP cases** where wording matters.

---

## ⚠️ Example 2: Provider Not Authorized (Patient *Is* Insured)

### ⚠️ Eligibility Result

**Patient appears insured, but eligibility could not be confirmed for your practice.**

---

### What This Means

* The patient likely **has coverage**
* The payer **did not allow eligibility to be returned for this practice**
* This is **not** a “no insurance” result

---

### Recommended Front-Desk Action

* Ask patient for insurance card
* Confirm network participation
* Proceed with caution for self-pay estimates

---

### Internal Classification (not shown to staff)

`ProviderNotAuthorized`

---

---

## ❌ Example 3: No Active Coverage Found

### ❌ Eligibility Result

**No active coverage found for this patient as of today.**

---

### What This Means

* The payer did not return active coverage
* This may indicate:

  * Coverage ended
  * Incorrect member information
  * Wrong payer selected

---

### Recommended Front-Desk Action

* Re-check insurance details
* Ask patient for updated coverage
* Discuss self-pay options if needed

---

### Internal Classification

`NoCoverage`

---

---

## ⏳ Example 4: Unable to Verify (System / Timeout)

### ⏳ Eligibility Result

**Eligibility could not be verified at this time.**

---

### What This Means

* The payer system did not respond
* This is a **temporary issue**
* Coverage status is unknown

---

### Recommended Front-Desk Action

* Retry later
* Ask patient for insurance card
* Do not assume no coverage

---

### Internal Classification

`Timeout` (retryable)

---

---

## Why this format is intentional (important)

This output:

✔ Separates **coverage status** from **provider authorization**
✔ Avoids false “no insurance” statements
✔ Uses plain language front desks already understand
✔ Minimizes legal and trust risk
✔ Maps cleanly to your ECE classification enums
✔ Can be rendered as:

* UI card
* PDF printout
* PMS note
* Audit record

---

## One-sentence internal rule (worth enforcing)

> **Front desk messaging must never imply “no insurance” unless ECE explicitly classifies `NoCoverage`.**

---

If you want next, I can:

* Design a **UI wireframe** for this output
* Create a **front-desk training one-pager**
* Map **ECE enums → exact display copy**
* Add a **“confidence indicator” UX pattern**

Just tell me which one you want.
