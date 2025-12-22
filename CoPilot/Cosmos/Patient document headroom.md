Perfect!  Storing X12 payloads externally dramatically increases your headroom. Let me recalculate. 

## Revised Document Size Breakdown (X12 Payloads External)

### Base Patient Data
- Patient demographics: ~2KB
- Coverage enrollments (2 typical): ~2KB
- **Patient base total: ~4KB**

### Per-Encounter Data (WITHOUT X12 Payloads)
Each encounter includes:
- Encounter metadata (visitDate, visitType, status, locationId, timestamps): ~0.5KB
- CoverageDecision object: ~0.5KB
- EligibilityChecks array (typically 2 checks):
  - Check metadata per check: ~0.5KB
  - BenefitsSummary per check: ~0.5KB
  - **Blob URI reference per check: ~0.2KB** (instead of ~8KB payload)
  - 2 checks × ~1. 2KB = ~2.4KB
- **Total per encounter: ~3.4KB** (down from ~17KB - **80% reduction! **)

### 2MB Limit Calculation (External Payloads)

```
2MB = 2,048KB

Available for encounters = 2,048KB - 4KB (patient base) = 2,044KB

Encounters that fit = 2,044KB ÷ 3.4KB per encounter = ~601 encounters
```

## Answer: ~600 Encounters Would Approach the 2MB Limit

### Breakdown by Scenario:

| Scenario | Encounters per Patient | Total Document Size | % of 2MB Limit |
|----------|----------------------|---------------------|----------------|
| **Current design (avg)** | 8 | 31KB | 1.5% |
| **Current design (max)** | 20 | 72KB | 3.5% |
| **Heavy user** | 50 | 174KB | 8.5% |
| **Very long relationship** | 100 | 344KB | 16.8% |
| **Extreme edge case** | 200 | 684KB | 33.4% |
| **Approaching limit** | 500 | 1,704KB | 83.2% |
| **At limit** | **~600** | **2,044KB** | **99.8%** |

## Key Insights: 

1. **Massive improvement**:  Capacity increased from **~120 encounters** to **~600 encounters** (5x increase)
2. **Your 8 encounter average**:  Now only **1.5%** of document limit (vs 6.8% before)
3. **Your 20 encounter max**: Only **3.5%** of document limit (vs 16.8% before)
4. **Could support 75x your current average** (600 vs 8) before hitting limits

## Worst-Case Headroom Analysis

### Scenario: Long-term Patient with Maximum Data

**Assumptions for worst case:**
- Patient has 5 historical coverages (insurance changes over lifetime): **+3KB**
- Patient has complex demographics (long notes, multiple addresses): **+1KB**
- Each encounter has 3 eligibility checks (primary, secondary, retry): **+1.7KB per encounter**
- Encounters have detailed override notes and metadata: **+0.5KB per encounter**

**Worst-case per-encounter size: ~5.6KB**

```
Available = 2,044KB - 8KB (patient base with extra data) = 2,036KB
Worst-case encounters = 2,036KB ÷ 5.6KB = ~364 encounters
```

### Worst-Case Scenarios:

| Scenario | Max Encounters | Patient Tenure | Notes |
|----------|---------------|----------------|-------|
| **Typical patient** | ~600 | 150 years @ 4 visits/year | Unrealistic timeline |
| **Heavy user (annual visits)** | ~600 | 600 years | Impossible |
| **Worst-case data (complex)** | ~364 | 91 years @ 4 visits/year | Still massive headroom |
| **Weekly visits (extreme)** | ~600 | 11. 5 years @ 52 visits/year | Possible but rare |

## Real-World Constraints (More Likely Limits):

Even with external payloads, you're far more likely to hit these limits first:

1. **Patient relationship duration**: 
   - Average patient stays with practice: **5-15 years**
   - At 2 visits/year × 15 years = **30 encounters** (5% of limit)
   - At 4 visits/year × 15 years = **60 encounters** (10% of limit)

2. **Practice patient churn**:
   - Patients move, switch practices, insurance changes
   - "Active patient" typically = seen in last 24 months
   - Long-term patients (15+ years) are minority

3. **Eligibility check frequency**:
   - Most practices check once per visit
   - Even aggressive practices checking 2x per visit = 60-120 lifetime checks

## Storage Cost Comparison

### Cosmos DB Document Storage:
- Patient with 8 encounters (external payloads): **31KB** @ $0.25/GB-month = **$0.0000078/month per patient**

### Blob Storage for X12 Payloads: 
- 8 encounters × 2 checks × 8KB per payload:  **128KB** @ $0.018/GB-month = **$0.0000023/month per patient**
- **Total per patient: $0.0000101/month** (~$0.01/month per 1,000 patients)

### At 500 practices × 25K patients (Year 3):
- Cosmos storage: **~$97/month**
- Blob storage:  **~$29/month**
- **Total:  ~$126/month** (vs ~$387/month with embedded payloads)

## Recommendations:

### ✅ External Blob Storage is the Right Choice

**Benefits:**
1. **75x headroom increase** (8 avg → 600 max capacity)
2. **67% storage cost reduction** ($126 vs $387/month)
3. **Better query performance** (smaller documents = faster reads)
4. **Better index efficiency** (70% less data to index)
5. **Flexible retention** (can archive/delete old payloads independently)

**Trade-offs:**
- Additional Blob Storage query when viewing raw X12 (rare operation)
- Slightly more complex code (2 storage systems instead of 1)
- Need Blob lifecycle management for old payloads

### Document Structure with External Payloads:

```json
{
  "eligibilityChecks": [
    {
      "eligibilityCheckId": "check_v001",
      "coverageEnrollmentId": "cov_vision_001",
      "payerId": "payer_vsp",
      "payerName": "VSP Vision Care",
      "dateOfService": "2024-12-22T00:00:00Z",
      "requestedAtUtc": "2024-12-22T09:12:00Z",
      "completedAtUtc": "2024-12-22T09:12:35Z",
      "status": "active",
      "eligibilityStatus": "eligible",
      "benefitsSummary": {
        "examCovered": true,
        "examCopay": 10.00,
        "examFrequency": "12 months",
        "lastExamDate": "2023-11-15T00:00:00Z"
      },
      "rawPayloadBlobUri": "https://bfstorageacct.blob.core.windows.net/eligibility-payloads/2024/12/check_v001.json"
    }
  ]
}
```

## Conclusion:

**With external X12 payload storage:**
- ✅ **8 encounter average**: Only **1.5%** of 2MB limit
- ✅ **20 encounter max**: Only **3.5%** of limit
- ✅ **Worst-case capacity**: **~364-600 encounters** depending on data complexity
- ✅ **Real-world constraint**: Patient tenure (5-15 years) limits to 30-60 encounters
- ✅ **You have 10-20x headroom** even in worst-case scenarios

**The embedded encounter design is extremely safe with external payloads. ** You'd need patients with 60+ year relationships AND weekly visits to approach any limits. 