┌──────────────────────────────────────────────────────────┐
│ Front Desk UI / Client                                   │
│                                                          │
│ POST /api/practices/{practiceId}/encounters/{encId}      │
│      /eligibility:run                                    │
│ Body: coverageEnrollmentId, dateOfService                │
└──────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────┐
│ ASP.NET Core Controller                                  │
│ EncounterEligibilityController                            │
│                                                          │
│ 1. Extract tenantId, userId from JWT claims               │
│ 2. Validate practiceId ∈ user practice claims             │
│ 3. Load TenantConfig (RequestTimeoutSeconds)              │
│ 4. Create linked CancellationToken                        │
│    (requestAborted + tenant timeout)                      │
│ 5. Build EligibilityRunCommand                            │
└──────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────┐
│ IEligibilityOrchestrator                                  │
│ (InProcessEligibilityOrchestrator)                        │
│                                                          │
│ • Thin seam for future Durable / queue orchestration      │
│ • Currently calls engine synchronously (async/await)     │
└──────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────┐
│ IEligibilityEngine.RunAsync                                │
│ (A → B → C → D pipeline)                                  │
│                                                          │
│ Compute IdempotencyKey                                    │
│ (encounter + enrollment + DOS)                            │
│                                                          │
│ Check encounter for existing matching eligibility         │
│ → if found & completed, return immediately                │
│                                                          │
│ Persist EligibilityCheck (Status = Running)               │
│ to Encounter (practice-scoped)                            │
└──────────────────────────────────────────────────────────┘
                         │
                         ▼
─────────────────────── A ──────────────────────────────────
│            Eligibility Execution (External IO)            │
┌──────────────────────────────────────────────────────────┐
│ IEligibilityProvider (Availity)                            │
│                                                          │
│ HttpClientFactory + Resilience Pipeline                   │
│  • Retry (exponential backoff + jitter)                   │
│  • Attempt timeout                                        │
│  • Total HTTP timeout                                     │
│  • Circuit breaker                                        │
│                                                          │
│ Async HTTP 270 → Availity → 271                            │
│                                                          │
│ If timeout/retry exhausted → throw                        │
└──────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────┐
│ IEligibilityPayloadStore                                   │
│                                                          │
│ • Store request + response payloads in Blob Storage       │
│ • Persist only payload references in Encounter            │
│   (no heavy indexing)                                     │
└──────────────────────────────────────────────────────────┘
                         │
                         ▼
─────────────────────── B ──────────────────────────────────
│            Parse + Normalize                               │
┌──────────────────────────────────────────────────────────┐
│ Eligibility Normalization Pipeline                        │
│                                                          │
│ • Parse X12 271                                           │
│ • Normalize payer-specific quirks                         │
│ • Apply payer behavior rules                              │
│ • Produce canonical coverage lines                        │
│                                                          │
│ Update EligibilityCheck with normalized data              │
└──────────────────────────────────────────────────────────┘
                         │
                         ▼
─────────────────────── C ──────────────────────────────────
│            Coverage Interpretation                         │
┌──────────────────────────────────────────────────────────┐
│ EligibilityInterpretationService                          │
│                                                          │
│ Inputs:                                                   │
│ • Encounter context (routine vs medical)                  │
│ • TenantConfig rules                                      │
│ • Practice PayerConfig                                    │
│ • Canonical coverage lines                                │
│                                                          │
│ Output:                                                   │
│ • Front-desk BenefitsSummary                              │
│ • Confidence / warning flags                              │
└──────────────────────────────────────────────────────────┘
                         │
                         ▼
─────────────────────── D ──────────────────────────────────
│            COB Decision Support                            │
┌──────────────────────────────────────────────────────────┐
│ ICobDecisionEngine                                        │
│                                                          │
│ Inputs:                                                   │
│ • Active coverage enrollments                             │
│ • Eligibility signals (active/inactive)                   │
│ • Tenant COB rules                                        │
│ • Practice payer defaults                                 │
│                                                          │
│ Output:                                                   │
│ • Primary / Secondary coverage decision                   │
│ • Reason codes                                            │
│                                                          │
│ Persist CoverageDecision to Encounter                     │
└──────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────┐
│ Finalize EligibilityCheck                                  │
│                                                          │
│ • Status = Succeeded | Failed | TimedOut | Canceled       │
│ • CompletedAtUtc                                          │
│                                                          │
│ Return EligibilityRunResult                               │
└──────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────┐
│ Controller maps Domain → DTO                              │
│                                                          │
│ HTTP 200 (or mapped error via middleware)                 │
│                                                          │
│ Response includes:                                        │
│ • EligibilityCheckId                                     │
│ • BenefitsSummary                                         │
│ • COB Decision                                            │
│ • Warnings                                                │
└──────────────────────────────────────────────────────────┘
