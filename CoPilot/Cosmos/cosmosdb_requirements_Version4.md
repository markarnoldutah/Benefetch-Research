# Azure Cosmos DB NoSQL Modeling Session

## Application Overview
- **Domain**: Healthcare SaaS - Insurance Eligibility, Benefits Verification, and Coordination of Benefits (COB) Decision Assistant
- **Primary Focus**:  Optometry practices (initial beachhead), with future expansion to ophthalmology, dental/orthodontics
- **Key Entities**:  Tenant, Practice, Patient, CoverageEnrollment (embedded), Encounter, EligibilityCheck (embedded), Payer, PayerConfig, TenantConfig, LookupSet, LookupItem
- **Business Context**: 
  - Multi-tenant SaaS platform
  - HIPAA-compliant PHI handling
  - Front-desk decision assistant for insurance eligibility and COB
  - Integrates with payer systems (Availity, etc.) for real-time eligibility checks
  - Designed for 1-10 doctor practices initially
  - Target customer:  Independent and small/mid-sized optometry groups
  - ~60-70% tenants have 1 practice, ~30-40% have multiple practices
  - Patient sharing across practices is rare - PHI is practice-scoped
  - Patient search is ALWAYS practice-scoped (never tenant-wide)
- **Scale**: 
  - **Per practice**: 1-10 ODs, 2,000-35,000 active patients, 15-250 check-ins/day
  - **Per OD**: 15-25 check-ins/day, 2,000-3,500 active patients
  - **Concurrent users**: 2-4 typical, 5-6 peak (per practice)
  - **Peak burst**: 30-50% of daily check-ins in 2-3 hour morning window (7: 30-10:30am local)
  - **Initial launch**:  Assume 50 practices onboarded in first year
  - **Growth**: 200 practices by year 2, 500 practices by year 3
- **Geographic Distribution**: Single region initially (US-based optometry practices), possible multi-region for data residency compliance later

## Primary Workflow (Patient Check-In for Appointment)
1. User creates new encounter, selecting type: vision or medical
2. User looks up Patient by firstname, lastname, dob (within practice context)
3. Upsert patient if necessary
4. Upsert patient coverage enrollment info and other eligibility request info
5. Persist encounter
6. Submit eligibility request to Availity for eligibility and COB response
7. Display response for user, offer to store as PDF blob

## Access Patterns Analysis

### Scale Calculations (for 10 OD practice - largest target)
- **Daily check-ins**: 150-250/day
- **Peak burst (7:30-10:30am)**: 75-125 check-ins in 3 hours = 180 minutes
- **Peak check-in rate**: ~0.42-0.69 per minute = ~0.007-0.012 per second
- **Concurrent users**: 4-6 users
- **Active patient base**: 20,000-35,000 patients per practice

| Pattern # | Description | RPS (Peak and Average) | Type | Attributes Needed | Key Requirements | Design Considerations | Status |
|-----------|-------------|-----------------|------|-------------------|------------------|----------------------|--------|
| 1 | Search patients by firstName, lastName, DOB during check-in | Peak: 0.05 RPS per practice (6 concurrent users × 0.008 searches/sec)<br>Avg: 0.01 RPS | Read | firstName, lastName, dateOfBirth, patientId, practiceId | <2s latency (user waiting)<br>Practice-scoped search | Single-partition query if PK=practiceId | ✅ |
| 2 | Get patient full details by patientId (after search) | Peak: 0.05 RPS per practice<br>Avg: 0.01 RPS | Read | patientId, all patient fields, embedded coverageEnrollments | <1s latency | Point read:  id + practiceId | ✅ |
| 3 | Create new patient during check-in (~40% of check-ins are new patients) | Peak: 0.02 RPS per practice<br>Avg: 0.004 RPS | Write | All patient fields | Strong consistency | Write to patients container | ✅ |
| 4 | Update existing patient demographics | Peak: 0.005 RPS per practice<br>Avg: 0.001 RPS | Write | Patient fields to update | Strong consistency | Full document replace in patients container (~4KB doc) | ✅ |
| 5 | Add coverage enrollment to patient | Peak: 0.03 RPS per practice<br>Avg: 0.006 RPS | Write | All coverage enrollment fields | Strong consistency | Update patient document (embedded array, ~1KB addition) | ✅ |
| 6 | Update coverage enrollment for patient | Peak: 0.02 RPS per practice<br>Avg: 0.004 RPS | Write | Coverage enrollment fields to update | Strong consistency | Update patient document (replace embedded array element, ~4KB doc) | ✅ |
| 7 | Delete coverage enrollment for patient | Peak: 0.001 RPS per practice<br>Avg: 0.0002 RPS | Write | coverageEnrollmentId | Strong consistency | Update patient document (remove from embedded array, ~4KB doc) | ✅ |
| 8 | Create new encounter for patient visit | Peak: 0.05 RPS per practice<br>Avg: 0.01 RPS | Write | All encounter fields | Strong consistency | Write to encounters container (~2KB initial doc) | ✅ |
| 9 | Get encounter by encounterId | Peak: 0.08 RPS per practice<br>Avg: 0.015 RPS | Read | encounterId, all encounter fields, embedded eligibilityChecks, embedded coverageDecision | <1s latency | Point read: id + practiceId | ✅ |
| 10 | Update encounter (add eligibility check results) | Peak: 0.05 RPS per practice (1-2 checks per encounter)<br>Avg: 0.01 RPS | Write | EligibilityCheck embedded object | Strong consistency | Update encounter document (add to embedded array, doc grows to ~10-20KB) | ✅ |
| 11 | Update encounter (add/update coverage decision) | Peak: 0.05 RPS per practice<br>Avg: 0.01 RPS | Write | CoverageDecision embedded object | Strong consistency | Update encounter document (update embedded object, ~10-20KB doc) | ✅ |
| 12 | Get patient's encounter history (for review/audit) | Peak: 0.01 RPS per practice<br>Avg: 0.002 RPS | Read | patientId, encounter summaries (id, date, type, status) | <3s latency<br>Return last 20 encounters | Query encounters by practiceId + patientId filter with composite index | ✅ |
| 13 | List all practices for a tenant (multi-practice tenants, admin view) | Peak: 0.001 RPS<br>Avg: 0.0002 RPS | Read | tenantId, practice list | <2s latency | Single-partition query if PK=tenantId | ✅ |
| 14 | Get tenant configuration (access gate checks, feature flags) | Peak: 0.1 RPS (checked frequently)<br>Avg: 0.02 RPS | Read | tenantId, all config fields | <500ms latency | Point read: id=tenantId, PK=tenantId | ✅ |
| 15 | Get payer catalog (global or tenant-specific) | Peak: 0.05 RPS<br>Avg: 0.01 RPS | Read | tenantId or "GLOBAL" | <1s latency | Point read or single-partition query | ✅ |
| 16 | Get lookup values (visit types, relationship codes, etc.) | Peak: 0.05 RPS<br>Avg: 0.01 RPS | Read | lookupSetId, tenant or global | <1s latency | Single-partition query | ✅ |

### Scale Calculations (platform-wide at maturity - 500 practices)
- **Year 3 scale**: 500 practices
- **Largest practices**:  Assume 50 practices with 10 ODs (high volume)
- **Platform-wide peak RPS estimates**:
  - Pattern #1 (patient search): 50 practices × 0.05 RPS = **2.5 RPS peak** (distributed across timezones)
  - Pattern #2 (get patient): 50 × 0.05 = **2.5 RPS peak**
  - Pattern #8 (create encounter): 50 × 0.05 = **2.5 RPS peak**
  - Pattern #10 (eligibility check): 50 × 0.05 = **2.5 RPS peak**
- **Total platform peak**:  ~**10-15 RPS** across all operations
- **RU consumption**: Very low - well within Cosmos DB serverless tier efficiency

## Entity Relationships Deep Dive
- **Tenant → Practices**: 1:Many (60-70% tenants have 1 practice, 30-40% have multiple)
- **Tenant → TenantConfig**: 1:1 (tenant-level configuration)
- **Tenant → PayerConfig**: 1:Many (tenant can configure multiple payers)
- **Practice → Patients**: 1:Many (practice has many patients, 2K-35K active patients per practice)
- **Patient → CoverageEnrollments**: 1:Many (embedded - patient can have vision + medical coverage, typically 1-2 coverages)
- **Patient → Encounters**: 1:Many (patient has multiple visits over time, ~1-2 visits per year typical, 50-100+ lifetime)
- **Encounter → EligibilityChecks**: 1:Many (embedded - encounter may check primary and secondary coverage, typically 1-2 checks)
- **Encounter → CoverageDecision**: 1:1 (embedded - which coverage to use for this visit)
- **Payer**:  Global master list (catalog)
- **LookupSet → LookupItems**: 1:Many (reference data)

## Enhanced Aggregate Analysis

### [Patient + CoverageEnrollments] Container Item Analysis
- **Access Correlation**: High - coverage info always needed with patient during check-in workflow
- **Query Patterns**:
  - Patient with coverages: ~90% of queries (check-in workflow step 2)
  - Update patient demographics only: ~10% of queries
  - Coverage-only operations: Never (coverages always in context of patient)
- **Size Constraints**: 
  - Patient base: ~2KB
  - Coverage enrollment: ~1KB each
  - Typical:  1-2 coverages per patient (vision + medical)
  - Combined max size: ~4KB typical, 10KB max (5 coverages edge case)
- **Update Patterns**: 
  - Patient demographics: Infrequent (address/phone changes, monthly at most)
  - Coverage enrollments: Moderate (insurance changes annually, updates quarterly)
  - Both updated independently
- **Current Design**:  Embedded in Patient document
- **Decision**:  ✅ Single Document (embedded)
- **Justification**: 
  - 90% access correlation during primary workflow
  - Bounded size (max 10KB well under 2MB limit)
  - Coverage never queried independently
  - Acceptable update coupling (both infrequent)
  - Low RPS (0.05 peak) means update amplification not a concern

### [Patient + Encounters] Container Item Analysis
- **Access Correlation**: Low - different access patterns
- **Query Patterns**:  
  - Patient only (during search/create): ~40% of queries
  - Encounter only (during visit workflow): ~50% of queries
  - Both together (patient history view): ~10% of queries
- **Size Constraints**: 
  - Patient: ~4KB with coverages
  - Encounters:  Unbounded growth (patient lifetime of visits, could be 100+ encounters over years)
  - Encounter size: ~2-10KB each depending on eligibility check payloads
  - Total encounters per patient: 50-200+ over lifetime = 100KB-2MB+ if aggregated
- **Update Patterns**:  
  - Patients: Infrequent updates
  - Encounters: Frequent creates (every visit), updates during visit workflow
  - Completely independent update patterns
- **Identifying Relationship**:  Encounters cannot exist without Patients, always have patientId when querying encounters
- **Current Design**: Separate containers
- **Decision**: ✅ Separate Containers
- **Justification**: 
  - Low access correlation (10%)
  - Unbounded growth on encounters side would exceed 2MB limit
  - Independent update patterns
  - Different indexing needs
  - Separate containers correct choice despite identifying relationship

### [Encounter + EligibilityChecks] Container Item Analysis
- **Access Correlation**: Very high - eligibility checks always accessed with encounter
- **Query Patterns**:  
  - Encounter with eligibility checks: ~95% of queries
  - Encounter without checks: ~5% (new encounter before submission)
  - Checks-only:  Never
- **Size Constraints**: 
  - Encounter base: ~2KB
  - EligibilityCheck:  ~2-8KB each (includes raw X12 payloads)
  - Typical: 1-2 checks per encounter (primary and possibly secondary)
  - Combined max size: ~20KB typical, edge case 50KB with multiple retries
- **Update Patterns**:  
  - Encounter created first, then eligibility checks added
  - Checks updated with results shortly after creation
  - Coverage decision updated based on check results
  - All updates within same user session (minutes)
- **Current Design**: Embedded in Encounter
- **Decision**: ✅ Single Document (embedded)
- **Justification**: 
  - 95% access correlation
  - Bounded size (50KB max well under 2MB)
  - Eligibility checks never queried independently
  - Transactional updates within same session
  - Atomic consistency needed for encounter + checks + decision
  - Low RPS (0.05 peak) means update amplification not a concern

## Container Consolidation Analysis

### Consolidation Decision Framework
After reviewing aggregates, current separate container design is appropriate. 

### Consolidation Candidates Review
| Parent | Child | Relationship | Access Overlap | Consolidation Decision | Justification |
|--------|-------|--------------|----------------|------------------------|---------------|
| Patient | Encounter | 1:Many | 10% | ❌ Separate | Unbounded growth (would exceed 2MB), independent update patterns, different indexing needs |

## CRITICAL Container-Specific Partition Key Strategy

### Design Principle
**Use the partition key that aligns with the natural query boundary for each container.**

- **PHI containers** (Patients, Encounters) → Partition by `practiceId` (all queries are practice-scoped)
- **Tenant operational containers** (Practices, TenantConfig, PayerConfig) → Partition by `tenantId` (tenant-scoped operations)
- **Global reference containers** (Payers, LookupSets) → Partition by `tenantId` or special "GLOBAL" value

### Container-by-Container Partition Key Decisions

#### Patients Container
- **Partition Key**:  `/practiceId` ✅
- **Reasoning**:
  - All patient queries are practice-scoped (Pattern #1, #2)
  - User is always in practice context during patient operations
  - Single-partition patient search (Pattern #1) saves RUs for multi-practice tenants
  - HIPAA alignment - PHI is practice-scoped
  - No tenant-wide patient queries exist or needed
- **Trade-offs Accepted**: None - this is the natural boundary
- **Documents**: Patient entities with embedded CoverageEnrollments

#### Encounters Container
- **Partition Key**: `/practiceId` ✅
- **Reasoning**:
  - All encounter queries are practice-scoped (Pattern #8, #9, #12)
  - User always in practice context during encounter workflow
  - Pattern #12 (patient encounter history) queries by practiceId + patientId filter
  - Better partition distribution at scale (500 practices vs ~200 tenants)
  - HIPAA alignment - encounter PHI is practice-scoped
- **Trade-offs Accepted**: None - this is the natural boundary
- **Documents**: Encounter entities with embedded EligibilityChecks and CoverageDecision

#### Practices Container
- **Partition Key**:  `/tenantId` ✅
- **Reasoning**: 
  - Pattern #13 (list practices) is tenant-scoped
  - Practice is an organizational entity, not PHI
  - Multi-practice tenants need efficient practice listing
  - Practice CRUD operations are tenant-admin operations
- **Trade-offs Accepted**: None - tenant boundary is natural for organizational data
- **Documents**: Practice entities

#### TenantConfig Container (or Tenants container if combined)
- **Partition Key**:  `/tenantId` ✅
- **Reasoning**: 
  - Pattern #14 (get tenant config) is tenant-scoped
  - Access gate checks, feature flags are tenant-level
  - High-frequency reads (0.1 RPS) need efficient point reads
  - No cross-tenant queries needed
- **Trade-offs Accepted**: None - tenant is the natural boundary
- **Documents**:  Tenant, TenantConfig entities

#### PayerConfig Container (or within Tenants container)
- **Partition Key**: `/tenantId` ✅
- **Reasoning**:
  - Payer configurations are tenant-specific (which payers enabled, COB rules, etc.)
  - Queried during eligibility workflows within tenant context
  - No cross-tenant payer config queries
- **Trade-offs Accepted**:  None
- **Documents**: PayerConfig entities

#### Payers Container (Global Catalog)
- **Partition Key**: `/tenantId` (with value "GLOBAL" for shared payers) ✅
- **Reasoning**: 
  - Pattern #15 - some payers are global (shared across all tenants)
  - Some tenants may have custom payer definitions
  - Using tenantId allows both: 
    - Global payers:  tenantId = "GLOBAL"
    - Tenant-specific payers: tenantId = specific tenant
  - Query pattern: Get payers for tenant = query where tenantId IN ("GLOBAL", specificTenantId)
- **Trade-offs Accepted**: 
  - Payer queries require checking both GLOBAL and tenant partitions (2 queries or cross-partition)
  - Low frequency (0.05 RPS) makes this acceptable
- **Alternative Considered**:  Separate partition key like `/payerScope`, but tenantId reuse is simpler
- **Documents**: Payer master entities

#### LookupSets Container (Reference Data)
- **Partition Key**: `/tenantId` ✅
- **Reasoning**: 
  - Pattern #16 - lookups can be global or tenant-overridden
  - Similar to Payers - use "GLOBAL" for shared lookups
  - Tenant-specific overrides use tenant's tenantId
  - Low frequency reads (0.05 RPS)
- **Trade-offs Accepted**: May require 2 queries (GLOBAL + tenant) but frequency is low
- **Documents**: LookupSet entities with embedded LookupItems

### Summary Table:  Partition Key Strategy

| Container | Partition Key | Reasoning | Documents per Partition (at maturity) |
|-----------|---------------|-----------|---------------------------------------|
| **Patients** | `/practiceId` | PHI, practice-scoped queries | 2K-35K patients per practice |
| **Encounters** | `/practiceId` | PHI, practice-scoped queries | 100K-1. 75M encounters per practice (50-100 per patient) |
| **Practices** | `/tenantId` | Organizational, tenant-scoped admin | 1-20 practices per tenant |
| **Tenants** | `/tenantId` | Tenant config, point reads | 1 document per tenant |
| **PayerConfig** | `/tenantId` | Tenant-specific payer settings | 10-50 payer configs per tenant |
| **Payers** | `/tenantId` | Global + tenant-specific catalog | 100-500 global payers, 0-20 per tenant |
| **LookupSets** | `/tenantId` | Global + tenant-specific reference | 20-50 lookup sets global, 0-10 per tenant |

### Physical Partition Projections (Year 3 - 500 practices)

**Patients Container** (PK = practiceId):
- Total data: 500 practices × 25K patients avg × 4KB = ~50GB
- Physical partitions: 50GB ÷ 50GB = **1 physical partition** (fits in single partition)
- Cross-partition overhead: None for patient queries ✅

**Encounters Container** (PK = practiceId):
- Total data: 500 practices × 25K patients × 50 encounters × 15KB = ~9.4TB
- Physical partitions: 9,400GB ÷ 50GB = **~188 physical partitions**
- Per practice (large): 35K patients × 100 encounters × 15KB = 52.5GB = **~2-3 physical partitions per large practice**
- Pattern #12 (patient encounter history) cross-partition impact: 
  - Small practice (1 OD): 1 physical partition = 2.5 RU overhead
  - Large practice (10 OD): 2-3 physical partitions = 5-7.5 RU overhead
  - **Mitigation**: Composite index on [practiceId, patientId, visitDate DESC], limit to recent 20 encounters

## Design Considerations

### Hot Partition Analysis
- ✅ **RESOLVED** - RPS is very low (0.05 per practice peak, ~10-15 RPS platform-wide at maturity)
- Even largest practices (10 ODs, 250 check-ins/day) generate <0.1 RPS
- No hot partition risk at this scale
- Partition by practiceId provides excellent distribution (500 partitions at maturity)
- Each logical partition stays well under 10,000 RU/s limit (most are <1 RU/s)

### Cross-Partition Query Costs
- **Patients Container** (PK = practiceId):
  - ✅ Pattern #1 (patient search): **Single-partition query** - 3-5 RU
  - ✅ Pattern #2 (get patient): **Point read** - 1 RU
  
- **Encounters Container** (PK = practiceId):
  - ✅ Pattern #8 (create encounter): **Point write** - 5 RU
  - ✅ Pattern #9 (get encounter): **Point read** - 1 RU
  - ⚠️ Pattern #12 (patient encounter history): **Cross-partition query within practice partition**
    - Small practice:  ~3 RU query + 2. 5 RU overhead = **5.5 RU**
    - Large practice: ~3 RU query + 5-7.5 RU overhead = **8.5-10.5 RU**
    - Frequency: 0.01 RPS = negligible cost impact
    - **Mitigation**: Composite index [practiceId ASC, patientId ASC, visitDate DESC], LIMIT 20
  
- **Payers/Lookups** (PK = tenantId):
  - ⚠️ Pattern #15, #16: May require 2 queries (GLOBAL + tenant) = ~5-10 RU total
  - Frequency: 0.05 RPS = negligible cost impact

### Indexing Strategy

#### Patients Container (PK = /practiceId)
**Indexing Mode**: Consistent (automatic)

**Composite Indexes**:
```json
{
  "compositeIndexes": [
    [
      { "path": "/practiceId", "order": "ascending" },
      { "path":  "/lastName", "order": "ascending" },
      { "path": "/firstName", "order": "ascending" },
      { "path": "/dateOfBirth", "order": "ascending" }
    ]
  ]
}
```

**Included Paths**:
- `/firstName/? `
- `/lastName/?`
- `/dateOfBirth/?`
- `/practiceId/?`
- `/email/?`
- `/phone/?`
- `/coverageEnrollments/*/payerId/?`
- `/coverageEnrollments/*/planType/?`
- `/coverageEnrollments/*/isActive/?`

**Excluded Paths**:
- `/coverageEnrollments/*/cobNotes/?` (large text)
- `/_etag/? ` (Cosmos metadata)

**Access Patterns Served**:  #1 (patient search), #2 (get patient)

**RU Impact**: 
- Pattern #1 with composite index: ~3-5 RU (vs 8-12 RU without index)
- Write overhead: +2 RU per patient write (acceptable at 0.02 RPS)

#### Encounters Container (PK = /practiceId)
**Indexing Mode**: Consistent (automatic)

**Composite Indexes**:
```json
{
  "compositeIndexes": [
    [
      { "path":  "/practiceId", "order":  "ascending" },
      { "path": "/patientId", "order": "ascending" },
      { "path": "/visitDate", "order": "descending" }
    ],
    [
      { "path": "/practiceId", "order": "ascending" },
      { "path": "/visitType", "order": "ascending" },
      { "path": "/visitDate", "order": "descending" }
    ]
  ]
}
```

**Included Paths**:
- `/patientId/?`
- `/visitDate/?`
- `/visitType/?`
- `/practiceId/?`
- `/eligibilityChecks/*/status/?`
- `/eligibilityChecks/*/payerId/?`
- `/coverageDecision/*/primaryCoverageEnrollmentId/?`

**Excluded Paths**: 
- `/eligibilityChecks/*/rawRequest/?` (large X12 payload)
- `/eligibilityChecks/*/rawResponse/?` (large X12 payload)
- `/coverageDecision/*/overrideNote/?` (large text)
- `/_etag/?`

**Access Patterns Served**:  #9 (get encounter), #12 (patient encounter history)

**RU Impact**:
- Pattern #12 with composite index: ~3-5 RU base + 2.5-7.5 RU overhead = 5.5-12.5 RU total
- Write overhead: +3 RU per encounter write (acceptable at 0.05 RPS)
- Storage savings:  Excluding large payloads saves ~60% index storage

#### Practices Container (PK = /tenantId)
**Indexing Mode**: Consistent (automatic)

**Included Paths**:  All (small documents, ~2KB each)

**Excluded Paths**: None needed

**Access Patterns Served**: #13 (list practices)

#### Tenants Container (PK = /tenantId)
**Indexing Mode**: Consistent (automatic)

**Included Paths**: All (point reads by tenantId, no complex queries)

**Access Patterns Served**: #14 (get tenant config)

#### Payers Container (PK = /tenantId)
**Indexing Mode**: Consistent (automatic)

**Composite Indexes**:
```json
{
  "compositeIndexes": [
    [
      { "path": "/tenantId", "order": "ascending" },
      { "path": "/payerName", "order": "ascending" }
    ]
  ]
}
```

**Access Patterns Served**: #15 (get payer catalog, list/search payers)

#### LookupSets Container (PK = /tenantId)
**Indexing Mode**: Consistent (automatic)

**Included Paths**: All (small documents with embedded items)

**Access Patterns Served**: #16 (get lookup values)

### Denormalization Strategy

#### ✅ APPROVED:  Patient Name in Encounter
**Fields to denormalize**:
- `patientFirstName` (string, ~20 bytes)
- `patientLastName` (string, ~30 bytes)

**Benefits**:
- Eliminates patient lookup when displaying encounter lists
- Saves 1 RU per encounter display (cross-container query avoided)
- Improves UX latency for encounter history views

**Costs**:
- +50 bytes per encounter document (negligible:  50 bytes / 15KB = 0.3% increase)
- Potential data staleness if patient changes name (rare:  legal name changes)
- Write amplification:  None (patient name added at encounter creation, not updated)

**Staleness Handling**:
- Accept stale patient names in historical encounters (audit trail benefit)
- Alternatively: Add background job to update encounter patient names when patient name changes (if needed)

**Implementation**:
```json
{
  "id": "enc_123",
  "practiceId": "prac_456",
  "patientId": "pat_789",
  "patientFirstName": "John",  // Denormalized
  "patientLastName": "Doe",    // Denormalized
  "visitDate": "2025-12-22",
  "visitType": "vision",
  ... 
}
```

#### ✅ APPROVED: Payer Name in EligibilityCheck
**Fields to denormalize**:
- `payerName` (string, ~50 bytes)

**Benefits**:
- Eliminates payer catalog lookup when displaying eligibility check results
- Saves cross-partition query (if payer is in different partition)
- Improves display performance for eligibility summaries

**Costs**: 
- +50 bytes per eligibility check (negligible)
- Potential data staleness if payer changes name (very rare)
- No write amplification (payer name added at check creation)

**Staleness Handling**:
- Accept stale payer names (reflects payer name at time of check)
- Historical accuracy benefit for audit trail

**Implementation** (within Encounter document):
```json
{
  "eligibilityChecks": [
    {
      "eligibilityCheckId": "check_001",
      "payerId": "payer_vsp",
      "payerName": "VSP Vision Care",  // Denormalized
      "status": "active",
      ... 
    }
  ]
}
```

### Multi-Document Opportunities
None identified - current aggregate boundaries are optimal.

### Global Distribution Strategy
- **Initial deployment**: Single region (East US 2 or West US 2)
- **Consistency level**: Session consistency (default)
  - Appropriate for web application with user sessions
  - Guarantees read-your-own-writes within session
  - Lower latency than Strong consistency
  - Sufficient for eligibility workflows (no multi-region coordination needed)
- **Future multi-region considerations**:
  - **Disaster recovery**: Enable geo-redundancy (read replicas in secondary region)
  - **Data residency**: If expanding to Canada/EU, may need regional data isolation
  - **Multi-region writes**: NOT needed (practices don't span regions, no conflict scenarios)
- **Conflict resolution**: N/A (single-region writes only)

### PHI Isolation & HIPAA Compliance
- ✅ All PHI entities (Patient, Encounter) inherit from `PracticeScopedEntityBase`
- ✅ Partition key strategy strengthens HIPAA isolation: 
  - Patients partitioned by `practiceId` = physical data isolation per practice
  - Encounters partitioned by `practiceId` = physical data isolation per practice
- ✅ All PHI queries require `practiceId` in claims validation
- ✅ No cross-practice PHI queries possible
- ✅ Audit trail:  `createdByUserId` tracked on all PHI documents

### Container Size Projections & Physical Partitions

**Year 3 Projections (500 practices)**:

| Container | Partition Key | Total Size | Physical Partitions | Notes |
|-----------|---------------|------------|-------------------|-------|
| Patients | practiceId | ~50 GB | 1 | Fits in single physical partition |
| Encounters | practiceId | ~9.4 TB | ~188 | 2-3 per large practice, cross-partition queries minimal overhead |
| Practices | tenantId | <1 GB | 1 | Small organizational data |
| Tenants | tenantId | <100 MB | 1 | Config documents only |
| PayerConfig | tenantId | <500 MB | 1 | Tenant payer settings |
| Payers | tenantId | <1 GB | 1 | Global catalog + tenant overrides |
| LookupSets | tenantId | <500 MB | 1 | Reference data |

**Total Platform Storage (Year 3)**: ~10 TB
- Dominated by Encounters container (historical eligibility data with X12 payloads)
- All other containers < 100 GB combined

## Validation Checklist
- [x] Application domain and scale documented ✅
- [x] All entities and relationships mapped ✅
- [x] Aggregate boundaries identified based on access patterns ✅
- [x] Identifying relationships checked for consolidation opportunities ✅
- [x] Container consolidation analysis completed ✅
- [x] Every access pattern has:  RPS (avg/peak), latency SLO, consistency level, expected result size ✅
- [x] Write pattern exists for every read pattern (and vice versa) ✅
- [x] Hot partition risks evaluated ✅
- [x] Consolidation framework applied; candidates reviewed ✅
- [x] Design considerations captured ✅
- [x] Partition key decision finalized (container-specific strategy) ✅
- [x] Indexing strategy validated ✅
- [x] Denormalization strategy confirmed ✅
- [ ] Ready to create final data model document ⏳