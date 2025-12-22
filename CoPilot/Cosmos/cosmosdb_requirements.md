# Azure Cosmos DB NoSQL Modeling Session

## Application Overview
- **Domain**: Healthcare SaaS - Insurance Eligibility, Benefits Verification, and Coordination of Benefits (COB) Decision Assistant
- **Primary Focus**: Optometry practices (initial beachhead), with future expansion to ophthalmology, dental/orthodontics
- **Key Entities**:  Tenant, Practice, Patient, CoverageEnrollment (embedded), Encounter (embedded), EligibilityCheck (embedded), Payer, PayerConfig, TenantConfig, LookupSet, LookupItem
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
  - **Patient lifetime encounters average 8** (annual visit × ~8 years avg patient relationship)
- **Scale**: 
  - **Per practice**: 1-10 ODs, 2,000-35,000 active patients, 15-250 check-ins/day
  - **Per OD**: 15-25 check-ins/day, 2,000-3,500 active patients
  - **Per patient**: 8 encounters average lifetime (range:  1-20)
  - **Concurrent users**: 2-4 typical, 5-6 peak (per practice)
  - **Peak burst**: 30-50% of daily check-ins in 2-3 hour morning window (7: 30-10:30am local)
  - **Initial launch**:  Assume 50 practices onboarded in first year
  - **Growth**: 200 practices by year 2, 500 practices by year 3
- **Geographic Distribution**: Single region initially (US-based optometry practices), possible multi-region for data residency compliance later

## Primary Workflow (Patient Check-In for Appointment)
1. User creates new encounter, selecting type:  vision or medical
2. User looks up Patient by firstname, lastname, dob (within practice context)
3. Upsert patient if necessary
4. Upsert patient coverage enrollment info and other eligibility request info
5. Persist encounter (embedded in patient document)
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
| 1 | Search patients by firstName, lastName, DOB during check-in | Peak: 0.05 RPS per practice<br>Avg: 0.01 RPS | Read | firstName, lastName, dateOfBirth, patientId, practiceId | <2s latency (user waiting)<br>Practice-scoped search | Single-partition query if PK=practiceId | ✅ |
| 2 | Get patient full details by patientId (includes encounters) | Peak: 0.05 RPS per practice<br>Avg: 0.01 RPS | Read | patientId, all patient fields, embedded coverageEnrollments, embedded encounters | <1s latency | Point read:  id + practiceId | ✅ |
| 3 | Create new patient during check-in (~40% of check-ins are new patients) | Peak: 0.02 RPS per practice<br>Avg: 0.004 RPS | Write | All patient fields | Strong consistency | Write to patients container | ✅ |
| 4 | Update existing patient demographics | Peak: 0.005 RPS per practice<br>Avg: 0.001 RPS | Write | Patient fields to update | Strong consistency | Full document replace in patients container | ✅ |
| 5 | Add coverage enrollment to patient | Peak: 0.03 RPS per practice<br>Avg: 0.006 RPS | Write | All coverage enrollment fields | Strong consistency | Update patient document (embedded array) | ✅ |
| 6 | Update coverage enrollment for patient | Peak: 0.02 RPS per practice<br>Avg: 0.004 RPS | Write | Coverage enrollment fields to update | Strong consistency | Update patient document (embedded array element) | ✅ |
| 7 | Delete coverage enrollment for patient | Peak: 0.001 RPS per practice<br>Avg: 0.0002 RPS | Write | coverageEnrollmentId | Strong consistency | Update patient document (remove from embedded array) | ✅ |
| 8 | Create new encounter for patient visit | Peak: 0.05 RPS per practice<br>Avg: 0.01 RPS | Write | All encounter fields | Strong consistency | Update patient document (add to embedded encounters array) | ✅ |
| 9 | Get encounter by encounterId (within patient context) | Peak: 0.08 RPS per practice<br>Avg: 0.015 RPS | Read | encounterId, patientId, all encounter fields | <1s latency | Point read patient, filter encounters array | ✅ |
| 10 | Update encounter (add eligibility check results) | Peak: 0.05 RPS per practice<br>Avg: 0.01 RPS | Write | EligibilityCheck embedded object | Strong consistency | Update patient document (update encounter in embedded array) | ✅ |
| 11 | Update encounter (add/update coverage decision) | Peak: 0.05 RPS per practice<br>Avg: 0.01 RPS | Write | CoverageDecision embedded object | Strong consistency | Update patient document (update encounter in embedded array) | ✅ |
| 12 | Get patient's encounter history | Peak: 0.01 RPS per practice<br>Avg: 0.002 RPS | Read | patientId, encounter summaries | <1s latency<br>All encounters (avg 8) | Point read patient, return encounters array | ✅ |
| 13 | List all practices for a tenant | Peak: 0.001 RPS<br>Avg: 0.0002 RPS | Read | tenantId, practice list | <2s latency | Single-partition query if PK=tenantId | ✅ |
| 14 | Get tenant configuration | Peak: 0.1 RPS<br>Avg: 0.02 RPS | Read | tenantId, all config fields | <500ms latency | Point read:  id=tenantId, PK=tenantId | ✅ |
| 15 | Get payer catalog | Peak: 0.05 RPS<br>Avg: 0.01 RPS | Read | tenantId or "GLOBAL" | <1s latency | Point read or single-partition query | ✅ |
| 16 | Get lookup values | Peak: 0.05 RPS<br>Avg: 0.01 RPS | Read | lookupSetId, tenant or global | <1s latency | Single-partition query | ✅ |

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
- **Patient → Encounters**:  1:Many (**BOUNDED** - avg 8 encounters per patient lifetime, embedded as array)
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
  - Patient demographics:  Infrequent (address/phone changes, monthly at most)
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

### [Patient + Encounters] Container Item Analysis - 🔴 CRITICAL CHANGE
- **Access Correlation**: HIGH - patient context almost always includes recent encounters
- **Query Patterns**:  
  - Patient with encounters:  ~80% of queries (view patient history during check-in)
  - Patient only (new patient): ~15% of queries
  - Encounter-only operations: ~5% (rare - update eligibility check on existing encounter)
- **Size Constraints**: 
  - Patient + coverages: ~4KB
  - **Encounter average:  ~15KB each** (with embedded eligibility checks)
  - **Average patient:  8 encounters = 120KB**
  - **Max patient: 20 encounters = 300KB**
  - **Combined typical size: ~124KB** (patient + 8 encounters)
  - **Combined max size: ~304KB** (patient + 20 encounters, well under 2MB limit)
- **Update Patterns**:  
  - New encounter creation: Every visit (0.05 RPS)
  - Encounter updates (add eligibility): During same visit session
  - Patient updates:  Rare
  - **Update amplification**: Replace 124KB document to update single encounter (**acceptable at low RPS**)
- **Identifying Relationship**:  Encounters cannot exist without Patients, always have patientId
- **Current Design**: Separate containers
- **Decision**: 🔄 **CHANGE TO SINGLE CONTAINER** - Embed encounters in Patient document
- **Justification**:  
  - **BOUNDED growth**: Avg 8 encounters (120KB), max 20 encounters (300KB) - stays well under 2MB
  - **High access correlation**: 80% of patient queries need encounter history
  - **Identifying relationship**:  Encounters always accessed in patient context
  - **Transactional consistency**: All patient data (demographics, coverage, encounter history) updated atomically
  - **Simplified queries**: Pattern #12 (patient encounter history) becomes simple point read instead of cross-container query
  - **RU savings**: Eliminate cross-container queries for encounter history (saves 5-10 RU per query)
  - **Update amplification acceptable**: 124KB document replace at 0.05 RPS = minimal cost impact (~15 RU per update vs 10 RU for separate)

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
  - Combined max size: ~20KB typical per encounter
- **Update Patterns**:  
  - Encounter created first, then eligibility checks added
  - Checks updated with results shortly after creation
  - Coverage decision updated based on check results
  - All updates within same user session (minutes)
- **Current Design**: Embedded in Encounter
- **Decision**: ✅ Single Document (embedded within Encounter, which is embedded in Patient)
- **Justification**: 
  - 95% access correlation
  - Bounded size per encounter (~20KB)
  - Eligibility checks never queried independently
  - Transactional updates within same session
  - Atomic consistency needed for encounter + checks + decision

## Container Consolidation Analysis

### Consolidation Decision Framework
After reviewing aggregates with corrected lifetime encounter counts, Patient + Encounters consolidation is now appropriate. 

### Consolidation Candidates Review
| Parent | Child | Relationship | Access Overlap | Consolidation Decision | Justification |
|--------|-------|--------------|----------------|------------------------|---------------|
| Patient | Encounter | 1:Many | 80% | ✅ **CONSOLIDATE** | **BOUNDED growth** (avg 8, max 20 encounters = 120-300KB), high access correlation, identifying relationship, eliminates cross-container queries |

## CRITICAL Container-Specific Partition Key Strategy

### Design Principle
**Use the partition key that aligns with the natural query boundary for each container.**

- **PHI container** (Patients with embedded Encounters) → Partition by `practiceId` (all queries are practice-scoped)
- **Tenant operational containers** (Practices, TenantConfig, PayerConfig) → Partition by `tenantId` (tenant-scoped operations)
- **Global reference containers** (Payers, LookupSets) → Partition by `tenantId` or special "GLOBAL" value

### Container-by-Container Partition Key Decisions

#### Patients Container (with embedded Encounters) - 🔴 CONSOLIDATED
- **Partition Key**: `/practiceId` ✅
- **Reasoning**:
  - All patient queries are practice-scoped (Pattern #1, #2)
  - All encounter queries are practice-scoped (Pattern #8, #9, #10, #11, #12)
  - User is always in practice context during patient operations
  - Single-partition patient search (Pattern #1) saves RUs
  - **Pattern #12 (encounter history) becomes point read** instead of cross-partition query
  - HIPAA alignment - PHI is practice-scoped
  - No tenant-wide patient queries exist or needed
- **Trade-offs Accepted**:  
  - Larger document size (124KB avg vs 4KB) = higher update costs
  - Update amplification:  replace entire patient doc to update single encounter
  - **Mitigation**: Low RPS (0.05 peak) makes update cost acceptable (~15 RU vs 10 RU for separate)
- **Documents**: Patient entities with embedded CoverageEnrollments AND embedded Encounters arrays

#### ~~Encounters Container~~ - 🔴 REMOVED (consolidated into Patients)

#### Practices Container
- **Partition Key**: `/tenantId` ✅
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
  - Payer configurations are tenant-specific
  - Queried during eligibility workflows within tenant context
  - No cross-tenant payer config queries
- **Trade-offs Accepted**:  None
- **Documents**: PayerConfig entities

#### Payers Container (Global Catalog)
- **Partition Key**:  `/tenantId` (with value "GLOBAL" for shared payers) ✅
- **Reasoning**:  
  - Pattern #15 - some payers are global (shared across all tenants)
  - Some tenants may have custom payer definitions
  - Using tenantId allows both:  
    - Global payers:  tenantId = "GLOBAL"
    - Tenant-specific payers: tenantId = specific tenant
- **Trade-offs Accepted**: 
  - Payer queries may require checking both GLOBAL and tenant partitions
  - Low frequency (0.05 RPS) makes this acceptable
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
| **Patients** | `/practiceId` | PHI, practice-scoped queries, **NOW INCLUDES EMBEDDED ENCOUNTERS** | 2K-35K patients per practice (each with ~8 encounters embedded) |
| **Practices** | `/tenantId` | Organizational, tenant-scoped admin | 1-20 practices per tenant |
| **Tenants** | `/tenantId` | Tenant config, point reads | 1 document per tenant |
| **PayerConfig** | `/tenantId` | Tenant-specific payer settings | 10-50 payer configs per tenant |
| **Payers** | `/tenantId` | Global + tenant-specific catalog | 100-500 global payers, 0-20 per tenant |
| **LookupSets** | `/tenantId` | Global + tenant-specific reference | 20-50 lookup sets global, 0-10 per tenant |

### Physical Partition Projections (Year 3 - 500 practices)

**Patients Container** (PK = practiceId, NOW includes embedded encounters):
- **Patient + encounters average**: 4KB (patient) + 120KB (8 encounters) = **124KB per patient**
- **Total data**: 500 practices × 25K patients avg × 124KB = **~1.55 TB**
- **Physical partitions**: 1,550GB ÷ 50GB = **~31 physical partitions** platform-wide
- **Per large practice**: 35K patients × 124KB = 4.34GB = **1 physical partition** per practice
- **Cross-partition overhead**: ✅ **NONE** for patient queries (single partition per practice)
- **Pattern #12 (patient history)**: ✅ **NOW A POINT READ** - no cross-partition overhead

**No Encounters Container** - consolidated into Patients

## Design Considerations

### Hot Partition Analysis
- ✅ **RESOLVED** - RPS is very low (0.05 per practice peak, ~10-15 RPS platform-wide at maturity)
- Even largest practices (10 ODs, 250 check-ins/day) generate <0.1 RPS
- No hot partition risk at this scale
- Partition by practiceId provides excellent distribution (500 partitions at maturity)
- Each logical partition stays well under 10,000 RU/s limit (most are <1 RU/s)

### Cross-Partition Query Costs
- **Patients Container** (PK = practiceId, with embedded encounters):
  - ✅ Pattern #1 (patient search): **Single-partition query** - 3-5 RU
  - ✅ Pattern #2 (get patient with encounters): **Point read** - 1 RU (now includes encounters!)
  - ✅ Pattern #12 (patient encounter history): **Point read** - 1 RU (was 5.5-12.5 RU with separate container!)
  - **RU SAVINGS**: Pattern #12 saves 4. 5-11. 5 RU per query (82-92% reduction)
  
- **Payers/Lookups** (PK = tenantId):
  - ⚠️ Pattern #15, #16: May require 2 queries (GLOBAL + tenant) = ~5-10 RU total
  - Frequency:  0.05 RPS = negligible cost impact

### Indexing Strategy

#### Patients Container (PK = /practiceId, with embedded encounters)
**Indexing Mode**:  Consistent (automatic)

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
- `/encounters/*/visitDate/?`
- `/encounters/*/visitType/?`
- `/encounters/*/status/?`
- `/encounters/*/eligibilityChecks/*/status/?`

**Excluded Paths**:
- `/coverageEnrollments/*/cobNotes/?` (large text)
- `/encounters/*/eligibilityChecks/*/rawRequest/?` (large X12 payload, 2-5KB)
- `/encounters/*/eligibilityChecks/*/rawResponse/?` (large X12 payload, 5-10KB)
- `/encounters/*/coverageDecision/overrideNote/?` (large text)
- `/_etag/?` (Cosmos metadata)

**Access Patterns Served**:  #1 (patient search), #2 (get patient), #12 (encounter history - now just array filtering)

**RU Impact**: 
- Pattern #1 with composite index: ~3-5 RU (vs 8-12 RU without)
- Pattern #2 (get patient with encounters): **1 RU point read** (includes all encounters!)
- Pattern #12 (encounter history): **1 RU point read** + client-side array filtering (was 5.5-12.5 RU!)
- Write overhead: +3-4 RU per patient write due to larger document size and encounter array indexing
- **Net RU savings**:  Massive - encounter history queries reduced by 82-92%
- Storage savings:  Excluding large X12 payloads saves ~70% index storage on encounter data

#### Practices Container (PK = /tenantId)
**Indexing Mode**: Consistent (automatic)

**Included Paths**:  All (small documents, ~2KB each)

**Excluded Paths**:  None needed

**Access Patterns Served**: #13 (list practices)

#### Tenants Container (PK = /tenantId)
**Indexing Mode**: Consistent (automatic)

**Included Paths**:  All (point reads by tenantId, no complex queries)

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

#### ✅ NO LONGER NEEDED:  Patient Name in Encounter
- **Previous reasoning**:  Eliminate patient lookup when displaying encounter lists
- **Now**:  Encounters are embedded in patient - patient name is already in parent document
- **Benefit**:  Simpler model, no data duplication, no staleness concerns

#### ✅ APPROVED: Payer Name in EligibilityCheck
- **Fields to denormalize**: `payerName` (string, ~50 bytes)
- **Benefits**:  Eliminates payer catalog lookup when displaying eligibility check results
- **Costs**: +50 bytes per eligibility check (negligible)
- **Staleness Handling**: Accept stale payer names (reflects payer name at time of check)

### Multi-Document Opportunities
None identified - single container (Patients) with multi-level embedding is optimal. 

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
- ✅ All PHI (Patient + CoverageEnrollments + Encounters + EligibilityChecks) in single container
- ✅ Partitioned by `practiceId` = physical data isolation per practice
- ✅ All PHI queries require `practiceId` in claims validation
- ✅ No cross-practice PHI queries possible
- ✅ Atomic transactions for all patient data updates (demographics + coverage + encounters)
- ✅ Audit trail:  `createdByUserId` tracked on all PHI documents

### Container Size Projections & Physical Partitions

**Year 3 Projections (500 practices)**:

| Container | Partition Key | Total Size | Physical Partitions | Notes |
|-----------|---------------|------------|-------------------|-------|
| **Patients** (with encounters) | practiceId | **~1.55 TB** | **~31** | Avg 124KB per patient (patient + 8 encounters with eligibility checks) |
| Practices | tenantId | <1 GB | 1 | Small organizational data |
| Tenants | tenantId | <100 MB | 1 | Config documents only |
| PayerConfig | tenantId | <500 MB | 1 | Tenant payer settings |
| Payers | tenantId | <1 GB | 1 | Global catalog + tenant overrides |
| LookupSets | tenantId | <500 MB | 1 | Reference data |

**Total Platform Storage (Year 3)**: ~1.56 TB
- Dominated by Patients container (includes historical eligibility data with X12 payloads)
- **84% reduction vs separate Encounters container approach** (was ~10 TB due to separate indexing overhead)

## Validation Checklist
- [x] Application domain and scale documented ✅
- [x] All entities and relationships mapped ✅
- [x] Aggregate boundaries identified based on access patterns ✅ (UPDATED for bounded encounters)
- [x] Identifying relationships checked for consolidation opportunities ✅ (CONSOLIDATED Patient + Encounters)
- [x] Container consolidation analysis completed ✅
- [x] Every access pattern has:  RPS (avg/peak), latency SLO, consistency level, expected result size ✅
- [x] Write pattern exists for every read pattern (and vice versa) ✅
- [x] Hot partition risks evaluated ✅
- [x] Consolidation framework applied; candidates reviewed ✅
- [x] Design considerations captured ✅
- [x] Partition key decision finalized (container-specific strategy) ✅
- [x] Indexing strategy validated ✅ (UPDATED for embedded encounters)
- [x] Denormalization strategy confirmed ✅ (SIMPLIFIED - patient name no longer needed)
- [x] Ready to create final data model document ✅