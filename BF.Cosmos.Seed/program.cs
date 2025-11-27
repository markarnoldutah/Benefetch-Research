using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using BF.Cosmos.Seed.DTOs;

// TODO: replace with your real values or pull from config
// private const string EndpointUri = "https://localhost:8081"; // or your Cosmos endpoint
const string EndpointUri = "https://bf-cosmos-dev-westus3.documents.azure.com:443/";

const string PrimaryKey = "REPLACE_WITH_YOUR_KEY";
const string DatabaseId = "bfdb";

const string TenantsContainerId = "tenants";
const string PracticesContainerId = "practices";
const string PatientsContainerId = "patients";
const string EncountersContainerId = "encounters";
const string PayersContainerId = "payers";


Console.WriteLine("Starting Cosmos seed...");

using var client = new CosmosClient(EndpointUri, PrimaryKey);

// 1. Ensure database & containers exist
var database = await client.CreateDatabaseIfNotExistsAsync(DatabaseId);

var tenantsContainer = await database.Database.CreateContainerIfNotExistsAsync(
    new ContainerProperties
    {
        Id = TenantsContainerId,
        PartitionKeyPath = "/id"
    });

var practicesContainer = await database.Database.CreateContainerIfNotExistsAsync(
    new ContainerProperties
    {
        Id = PracticesContainerId,
        PartitionKeyPath = "/tenantId"
    });

var patientsContainer = await database.Database.CreateContainerIfNotExistsAsync(
    new ContainerProperties
    {
        Id = PatientsContainerId,
        PartitionKeyPath = "/id"
    });

var encountersContainer = await database.Database.CreateContainerIfNotExistsAsync(
    new ContainerProperties
    {
        Id = EncountersContainerId,
        PartitionKeyPath = "/patientId"
    });

var payersContainer = await database.Database.CreateContainerIfNotExistsAsync(
    new ContainerProperties
    {
        Id = PayersContainerId,
        PartitionKeyPath = "/id"
    });

// 2. Build sample data

var tenants = BuildTenants();
var practices = BuildPractices();
var payers = BuildPayers();
var patients = BuildPatients();
var encounters = BuildEncounters();

// 3. Seed data

Console.WriteLine("Seeding tenants...");
foreach (var t in tenants)
{
    await tenantsContainer.Container.UpsertItemAsync(t, new PartitionKey(t.Id));
}

Console.WriteLine("Seeding practices...");
foreach (var p in practices)
{
    await practicesContainer.Container.UpsertItemAsync(p, new PartitionKey(p.TenantId));
}

Console.WriteLine("Seeding payers...");
foreach (var p in payers)
{
    await payersContainer.Container.UpsertItemAsync(p, new PartitionKey(p.Id));
}

Console.WriteLine("Seeding patients...");
foreach (var p in patients)
{
    await patientsContainer.Container.UpsertItemAsync(p, new PartitionKey(p.Id));
}

Console.WriteLine("Seeding encounters...");
foreach (var e in encounters)
{
    await encountersContainer.Container.UpsertItemAsync(e, new PartitionKey(e.PatientId));
}

Console.WriteLine("Done seeding.");


#region Builders

static List<TenantDocument> BuildTenants() =>
    new()
    {
                new TenantDocument
                {
                    Id = "ten_abc",
                    TenantId = "ten_abc",
                    Type = "tenant",
                    Name = "Acme Eye Group",
                    BillingEmail = "admin@acmeeye.com",
                    Status = "Active",
                    Plan = "Pro",
                    Notes = "Early adopter tenant",
                    CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAtUtc = new DateTime(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc)
                },
                new TenantDocument
                {
                    Id = "ten_xyz",
                    TenantId = "ten_xyz",
                    Type = "tenant",
                    Name = "VisionCare Network",
                    BillingEmail = "billing@visioncare.com",
                    Status = "Active",
                    Plan = "Standard",
                    Notes = null,
                    CreatedAtUtc = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAtUtc = new DateTime(2025, 3, 5, 9, 30, 0, DateTimeKind.Utc)
                }
    };

static List<PracticeDocument> BuildPractices() =>
    new()
    {
                new PracticeDocument
                {
                    Id = "prac_001",
                    PracticeId = "prac_001",
                    Type = "practice",
                    TenantId = "ten_abc",
                    Name = "Acme Eye – Downtown",
                    ExternalRef = "legacyPracId-123",
                    IsActive = true,
                    Phone = "555-111-2222",
                    Email = "downtown@acmeeye.com",
                    Locations = new List<LocationEmbedded>
                    {
                        new LocationEmbedded
                        {
                            LocationId = "loc_001",
                            Name = "Downtown Clinic – Main",
                            Address1 = "123 Main St",
                            Address2 = null,
                            City = "Denver",
                            State = "CO",
                            PostalCode = "80202",
                            Phone = "555-111-2222",
                            IsActive = true
                        },
                        new LocationEmbedded
                        {
                            LocationId = "loc_002",
                            Name = "Downtown Clinic – Annex",
                            Address1 = "200 Side St",
                            Address2 = "Suite 300",
                            City = "Denver",
                            State = "CO",
                            PostalCode = "80202",
                            Phone = "555-222-3333",
                            IsActive = true
                        }
                    },
                    CreatedAtUtc = new DateTime(2025, 1, 5, 8, 0, 0, DateTimeKind.Utc),
                    UpdatedAtUtc = new DateTime(2025, 3, 2, 10, 15, 0, DateTimeKind.Utc)
                },
                new PracticeDocument
                {
                    Id = "prac_002",
                    PracticeId = "prac_002",
                    Type = "practice",
                    TenantId = "ten_abc",
                    Name = "Acme Eye – Suburb",
                    ExternalRef = "legacyPracId-456",
                    IsActive = true,
                    Phone = "555-444-5555",
                    Email = "suburb@acmeeye.com",
                    Locations = new List<LocationEmbedded>
                    {
                        new LocationEmbedded
                        {
                            LocationId = "loc_003",
                            Name = "Suburb Clinic",
                            Address1 = "789 Lakeview Dr",
                            Address2 = null,
                            City = "Aurora",
                            State = "CO",
                            PostalCode = "80012",
                            Phone = "555-444-5555",
                            IsActive = true
                        }
                    },
                    CreatedAtUtc = new DateTime(2025, 1, 10, 9, 0, 0, DateTimeKind.Utc),
                    UpdatedAtUtc = new DateTime(2025, 3, 3, 11, 20, 0, DateTimeKind.Utc)
                },
                new PracticeDocument
                {
                    Id = "prac_101",
                    PracticeId = "prac_101",
                    Type = "practice",
                    TenantId = "ten_xyz",
                    Name = "VisionCare – Central",
                    ExternalRef = null,
                    IsActive = true,
                    Phone = "555-777-8888",
                    Email = "central@visioncare.com",
                    Locations = new List<LocationEmbedded>
                    {
                        new LocationEmbedded
                        {
                            LocationId = "loc_101",
                            Name = "Central Clinic",
                            Address1 = "10 Center Blvd",
                            Address2 = "Floor 2",
                            City = "Boulder",
                            State = "CO",
                            PostalCode = "80301",
                            Phone = "555-777-8888",
                            IsActive = true
                        }
                    },
                    CreatedAtUtc = new DateTime(2025, 2, 5, 10, 0, 0, DateTimeKind.Utc),
                    UpdatedAtUtc = new DateTime(2025, 3, 4, 9, 45, 0, DateTimeKind.Utc)
                }
    };

static List<PayerDocument> BuildPayers() =>
    new()
    {
                new PayerDocument
                {
                    Id = "payer_vsp_001",
                    PayerId = "payer_vsp_001",
                    Type = "payer",
                    TenantId = null,
                    Name = "VSP",
                    PlanType = "Vision",
                    AvailityPayerCode = "VSP",
                    X12PayerId = "12345",
                    IsMedicare = false,
                    IsMedicaid = false,
                    CreatedAtUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAtUtc = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc)
                },
                new PayerDocument
                {
                    Id = "payer_bcbs_001",
                    PayerId = "payer_bcbs_001",
                    Type = "payer",
                    TenantId = null,
                    Name = "Blue Cross Blue Shield",
                    PlanType = "Medical",
                    AvailityPayerCode = "BCBS",
                    X12PayerId = "67890",
                    IsMedicare = false,
                    IsMedicaid = false,
                    CreatedAtUtc = new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAtUtc = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc)
                },
                new PayerDocument
                {
                    Id = "payer_flblue_001",
                    PayerId = "payer_flblue_001",
                    Type = "payer",
                    TenantId = "ten_abc",
                    Name = "Florida Blue",
                    PlanType = "Medical",
                    AvailityPayerCode = "FLBLUE",
                    X12PayerId = "44556",
                    IsMedicare = false,
                    IsMedicaid = false,
                    CreatedAtUtc = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAtUtc = new DateTime(2024, 6, 20, 11, 0, 0, DateTimeKind.Utc)
                }
    };

static List<PatientDocument> BuildPatients() =>
    new()
    {
                // Jane Doe
                new PatientDocument
                {
                    Id = "pat_123",
                    PatientId = "pat_123",
                    Type = "patient",
                    TenantId = "ten_abc",
                    PracticeId = "prac_001",
                    FirstName = "Jane",
                    LastName = "Doe",
                    DateOfBirth = new DateTime(1985, 2, 10, 0, 0, 0, DateTimeKind.Utc),
                    Email = "jane.doe@example.com",
                    Phone = "555-123-4567",
                    CoverageEnrollments = new List<CoverageEnrollmentEmbedded>
                    {
                        new CoverageEnrollmentEmbedded
                        {
                            CoverageEnrollmentId = "cov_vision_1",
                            PayerId = "payer_vsp_001",
                            PlanType = "Vision",
                            MemberId = "VSP12345",
                            GroupNumber = "GRP987",
                            RelationshipToSubscriber = "Self",
                            SubscriberFirstName = "Jane",
                            SubscriberLastName = "Doe",
                            SubscriberDob = new DateTime(1985, 2, 10, 0, 0, 0, DateTimeKind.Utc),
                            IsEmployerPlan = true,
                            IsVisionPlan = true,
                            IsMedicalPlan = false,
                            EffectiveDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            TerminationDate = null,
                            IsActive = true,
                            CobPriorityHint = 1,
                            IsCobLocked = false,
                            CobNotes = "Use as primary for routine vision."
                        },
                        new CoverageEnrollmentEmbedded
                        {
                            CoverageEnrollmentId = "cov_med_1",
                            PayerId = "payer_bcbs_001",
                            PlanType = "Medical",
                            MemberId = "BCBS56789",
                            GroupNumber = "PLN444",
                            RelationshipToSubscriber = "Self",
                            SubscriberFirstName = "Jane",
                            SubscriberLastName = "Doe",
                            SubscriberDob = new DateTime(1985, 2, 10, 0, 0, 0, DateTimeKind.Utc),
                            IsEmployerPlan = true,
                            IsVisionPlan = false,
                            IsMedicalPlan = true,
                            EffectiveDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            TerminationDate = null,
                            IsActive = true,
                            CobPriorityHint = 2,
                            IsCobLocked = false,
                            CobNotes = null
                        }
                    },
                    CreatedAtUtc = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc),
                    UpdatedAtUtc = new DateTime(2025, 3, 10, 9, 45, 0, DateTimeKind.Utc)
                },

                // Mark Smith
                new PatientDocument
                {
                    Id = "pat_124",
                    PatientId = "pat_124",
                    Type = "patient",
                    TenantId = "ten_abc",
                    PracticeId = "prac_002",
                    FirstName = "Mark",
                    LastName = "Smith",
                    DateOfBirth = new DateTime(1990, 7, 20, 0, 0, 0, DateTimeKind.Utc),
                    Email = "mark.smith@example.com",
                    Phone = "555-234-5678",
                    CoverageEnrollments = new List<CoverageEnrollmentEmbedded>
                    {
                        new CoverageEnrollmentEmbedded
                        {
                            CoverageEnrollmentId = "cov_med_2",
                            PayerId = "payer_flblue_001",
                            PlanType = "Medical",
                            MemberId = "FLB99887",
                            GroupNumber = "FLG123",
                            RelationshipToSubscriber = "Self",
                            SubscriberFirstName = "Mark",
                            SubscriberLastName = "Smith",
                            SubscriberDob = new DateTime(1990, 7, 20, 0, 0, 0, DateTimeKind.Utc),
                            IsEmployerPlan = false,
                            IsVisionPlan = false,
                            IsMedicalPlan = true,
                            EffectiveDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                            TerminationDate = null,
                            IsActive = true,
                            CobPriorityHint = 1,
                            IsCobLocked = false,
                            CobNotes = "Primary medical"
                        }
                    },
                    CreatedAtUtc = new DateTime(2025, 2, 1, 10, 0, 0, DateTimeKind.Utc),
                    UpdatedAtUtc = new DateTime(2025, 3, 8, 14, 20, 0, DateTimeKind.Utc)
                },

                // Emily Wong
                new PatientDocument
                {
                    Id = "pat_555",
                    PatientId = "pat_555",
                    Type = "patient",
                    TenantId = "ten_xyz",
                    PracticeId = "prac_101",
                    FirstName = "Emily",
                    LastName = "Wong",
                    DateOfBirth = new DateTime(2005, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                    Email = "emily.wong@example.com",
                    Phone = "555-345-6789",
                    CoverageEnrollments = new List<CoverageEnrollmentEmbedded>
                    {
                        new CoverageEnrollmentEmbedded
                        {
                            CoverageEnrollmentId = "cov_vision_555",
                            PayerId = "payer_vsp_001",
                            PlanType = "Vision",
                            MemberId = "VSP55555",
                            GroupNumber = "GRP555",
                            RelationshipToSubscriber = "Child",
                            SubscriberFirstName = "Linda",
                            SubscriberLastName = "Wong",
                            SubscriberDob = new DateTime(1978, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                            IsEmployerPlan = true,
                            IsVisionPlan = true,
                            IsMedicalPlan = false,
                            EffectiveDate = new DateTime(2023, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                            TerminationDate = null,
                            IsActive = true,
                            CobPriorityHint = 1,
                            IsCobLocked = false,
                            CobNotes = null
                        }
                    },
                    CreatedAtUtc = new DateTime(2025, 2, 10, 11, 30, 0, DateTimeKind.Utc),
                    UpdatedAtUtc = new DateTime(2025, 3, 9, 16, 10, 0, DateTimeKind.Utc)
                }
    };

static List<EncounterDocument> BuildEncounters() =>
    new()
    {
                // Encounter 1 - Jane, dual coverage, routine vision
                new EncounterDocument
                {
                    Id = "enc_001",
                    EncounterId = "enc_001",
                    Type = "encounter",
                    TenantId = "ten_abc",
                    PracticeId = "prac_001",
                    LocationId = "loc_001",
                    PatientId = "pat_123",
                    VisitDate = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                    VisitType = "RoutineVision",
                    ExternalRef = "appt_789",
                    CoverageDecision = new CoverageDecisionEmbedded
                    {
                        EncounterCoverageDecisionId = "cob_dec_001",
                        PrimaryCoverageEnrollmentId = "cov_vision_1",
                        SecondaryCoverageEnrollmentId = "cov_med_1",
                        CobReason = "RoutineVision_UseVisionPlanPrimary",
                        OverriddenByUser = false,
                        OverrideNote = null,
                        CreatedAtUtc = new DateTime(2025, 3, 15, 15, 1, 0, DateTimeKind.Utc),
                        CreatedByUserId = "user_frontdesk_01"
                    },
                    EligibilityChecks = new List<EligibilityCheckEmbedded>
                    {
                        new EligibilityCheckEmbedded
                        {
                            EligibilityCheckId = "elig_001",
                            CoverageEnrollmentId = "cov_vision_1",
                            PayerId = "payer_vsp_001",
                            DateOfService = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                            RequestedAtUtc = new DateTime(2025, 3, 15, 15, 2, 0, DateTimeKind.Utc),
                            CompletedAtUtc = new DateTime(2025, 3, 15, 15, 2, 3, DateTimeKind.Utc),
                            Status = "Succeeded",
                            RawStatusCode = "1",
                            RawStatusDescription = "Active Coverage",
                            MemberIdSnapshot = "VSP12345",
                            GroupNumberSnapshot = "GRP987",
                            PlanNameSnapshot = "VSP Advantage Plan",
                            EffectiveDateSnapshot = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            TerminationDateSnapshot = null,
                            ErrorMessage = null,
                            CoverageLines = new List<CoverageLineEmbedded>
                            {
                                new CoverageLineEmbedded
                                {
                                    ServiceTypeCode = "47",
                                    CoverageDescription = "Vision Exam",
                                    CopayAmount = 10m,
                                    CoinsurancePercent = null,
                                    DeductibleAmount = null,
                                    RemainingDeductible = null,
                                    OutOfPocketMax = null,
                                    RemainingOutOfPocket = null,
                                    AllowanceAmount = null,
                                    NetworkIndicator = "IN",
                                    EffectiveDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    TerminationDate = null,
                                    AdditionalInfo = "Exam every 12 months"
                                },
                                new CoverageLineEmbedded
                                {
                                    ServiceTypeCode = "47",
                                    CoverageDescription = "Frames",
                                    CopayAmount = null,
                                    CoinsurancePercent = null,
                                    DeductibleAmount = null,
                                    RemainingDeductible = null,
                                    OutOfPocketMax = null,
                                    RemainingOutOfPocket = null,
                                    AllowanceAmount = 150m,
                                    NetworkIndicator = "IN",
                                    EffectiveDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    TerminationDate = null,
                                    AdditionalInfo = "Frames every 24 months"
                                }
                            },
                            Payloads = new List<EligibilityPayloadEmbedded>
                            {
                                new EligibilityPayloadEmbedded
                                {
                                    PayloadId = "pay_req_001",
                                    Direction = "Request",
                                    Format = "X12_270",
                                    StorageUrl = "https://storage/account/container/elig_001_req.x12",
                                    CreatedAtUtc = new DateTime(2025, 3, 15, 15, 2, 0, DateTimeKind.Utc)
                                },
                                new EligibilityPayloadEmbedded
                                {
                                    PayloadId = "pay_res_001",
                                    Direction = "Response",
                                    Format = "X12_271",
                                    StorageUrl = "https://storage/account/container/elig_001_res.x12",
                                    CreatedAtUtc = new DateTime(2025, 3, 15, 15, 2, 3, DateTimeKind.Utc)
                                }
                            }
                        },
                        new EligibilityCheckEmbedded
                        {
                            EligibilityCheckId = "elig_002",
                            CoverageEnrollmentId = "cov_med_1",
                            PayerId = "payer_bcbs_001",
                            DateOfService = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                            RequestedAtUtc = new DateTime(2025, 3, 15, 15, 3, 0, DateTimeKind.Utc),
                            CompletedAtUtc = new DateTime(2025, 3, 15, 15, 3, 2, DateTimeKind.Utc),
                            Status = "Succeeded",
                            RawStatusCode = "1",
                            RawStatusDescription = "Active Coverage",
                            MemberIdSnapshot = "BCBS56789",
                            GroupNumberSnapshot = "PLN444",
                            PlanNameSnapshot = "BCBS PPO",
                            EffectiveDateSnapshot = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            TerminationDateSnapshot = null,
                            ErrorMessage = null,
                            CoverageLines = new List<CoverageLineEmbedded>
                            {
                                new CoverageLineEmbedded
                                {
                                    ServiceTypeCode = "30",
                                    CoverageDescription = "Health Benefit Plan Coverage",
                                    CopayAmount = 30m,
                                    CoinsurancePercent = 20m,
                                    DeductibleAmount = 1000m,
                                    RemainingDeductible = 600m,
                                    OutOfPocketMax = 3000m,
                                    RemainingOutOfPocket = 1800m,
                                    AllowanceAmount = null,
                                    NetworkIndicator = "IN",
                                    EffectiveDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    TerminationDate = null,
                                    AdditionalInfo = null
                                }
                            },
                            Payloads = new List<EligibilityPayloadEmbedded>()
                        }
                    },
                    CreatedAtUtc = new DateTime(2025, 3, 15, 15, 0, 0, DateTimeKind.Utc),
                    CreatedByUserId = "user_frontdesk_01"
                },

                // Encounter 2 - Jane, medical visit
                new EncounterDocument
                {
                    Id = "enc_002",
                    EncounterId = "enc_002",
                    Type = "encounter",
                    TenantId = "ten_abc",
                    PracticeId = "prac_001",
                    LocationId = "loc_002",
                    PatientId = "pat_123",
                    VisitDate = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                    VisitType = "Medical",
                    ExternalRef = "appt_812",
                    CoverageDecision = new CoverageDecisionEmbedded
                    {
                        EncounterCoverageDecisionId = "cob_dec_002",
                        PrimaryCoverageEnrollmentId = "cov_med_1",
                        SecondaryCoverageEnrollmentId = "cov_vision_1",
                        CobReason = "MedicalVisit_UseMedicalPlanPrimary",
                        OverriddenByUser = false,
                        OverrideNote = null,
                        CreatedAtUtc = new DateTime(2025, 4, 1, 14, 1, 0, DateTimeKind.Utc),
                        CreatedByUserId = "user_frontdesk_02"
                    },
                    EligibilityChecks = new List<EligibilityCheckEmbedded>
                    {
                        new EligibilityCheckEmbedded
                        {
                            EligibilityCheckId = "elig_003",
                            CoverageEnrollmentId = "cov_med_1",
                            PayerId = "payer_bcbs_001",
                            DateOfService = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                            RequestedAtUtc = new DateTime(2025, 4, 1, 14, 2, 0, DateTimeKind.Utc),
                            CompletedAtUtc = new DateTime(2025, 4, 1, 14, 2, 2, DateTimeKind.Utc),
                            Status = "Succeeded",
                            RawStatusCode = "1",
                            RawStatusDescription = "Active Coverage",
                            MemberIdSnapshot = "BCBS56789",
                            GroupNumberSnapshot = "PLN444",
                            PlanNameSnapshot = "BCBS PPO",
                            EffectiveDateSnapshot = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            TerminationDateSnapshot = null,
                            ErrorMessage = null,
                            CoverageLines = new List<CoverageLineEmbedded>
                            {
                                new CoverageLineEmbedded
                                {
                                    ServiceTypeCode = "30",
                                    CoverageDescription = "Health Benefit Plan Coverage",
                                    CopayAmount = 25m,
                                    CoinsurancePercent = 20m,
                                    DeductibleAmount = 1000m,
                                    RemainingDeductible = 550m,
                                    OutOfPocketMax = 3000m,
                                    RemainingOutOfPocket = 1700m,
                                    AllowanceAmount = null,
                                    NetworkIndicator = "IN",
                                    EffectiveDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                    TerminationDate = null,
                                    AdditionalInfo = "PCP visit"
                                }
                            },
                            Payloads = new List<EligibilityPayloadEmbedded>()
                        }
                    },
                    CreatedAtUtc = new DateTime(2025, 4, 1, 14, 0, 0, DateTimeKind.Utc),
                    CreatedByUserId = "user_frontdesk_02"
                },

                // Encounter 3 - Mark, medical
                new EncounterDocument
                {
                    Id = "enc_010",
                    EncounterId = "enc_010",
                    Type = "encounter",
                    TenantId = "ten_abc",
                    PracticeId = "prac_002",
                    LocationId = "loc_003",
                    PatientId = "pat_124",
                    VisitDate = new DateTime(2025, 3, 20, 0, 0, 0, DateTimeKind.Utc),
                    VisitType = "Medical",
                    ExternalRef = "appt_990",
                    CoverageDecision = new CoverageDecisionEmbedded
                    {
                        EncounterCoverageDecisionId = "cob_dec_010",
                        PrimaryCoverageEnrollmentId = "cov_med_2",
                        SecondaryCoverageEnrollmentId = null,
                        CobReason = "SingleCoverage_PrimaryMedical",
                        OverriddenByUser = false,
                        OverrideNote = null,
                        CreatedAtUtc = new DateTime(2025, 3, 20, 16, 1, 0, DateTimeKind.Utc),
                        CreatedByUserId = "user_frontdesk_03"
                    },
                    EligibilityChecks = new List<EligibilityCheckEmbedded>
                    {
                        new EligibilityCheckEmbedded
                        {
                            EligibilityCheckId = "elig_010",
                            CoverageEnrollmentId = "cov_med_2",
                            PayerId = "payer_flblue_001",
                            DateOfService = new DateTime(2025, 3, 20, 0, 0, 0, DateTimeKind.Utc),
                            RequestedAtUtc = new DateTime(2025, 3, 20, 16, 2, 0, DateTimeKind.Utc),
                            CompletedAtUtc = new DateTime(2025, 3, 20, 16, 2, 2, DateTimeKind.Utc),
                            Status = "Succeeded",
                            RawStatusCode = "1",
                            RawStatusDescription = "Active Coverage",
                            MemberIdSnapshot = "FLB99887",
                            GroupNumberSnapshot = "FLG123",
                            PlanNameSnapshot = "Florida Blue PPO",
                            EffectiveDateSnapshot = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                            TerminationDateSnapshot = null,
                            ErrorMessage = null,
                            CoverageLines = new List<CoverageLineEmbedded>
                            {
                                new CoverageLineEmbedded
                                {
                                    ServiceTypeCode = "30",
                                    CoverageDescription = "Health Benefit Plan Coverage",
                                    CopayAmount = 20m,
                                    CoinsurancePercent = 10m,
                                    DeductibleAmount = 500m,
                                    RemainingDeductible = 400m,
                                    OutOfPocketMax = 2500m,
                                    RemainingOutOfPocket = 2200m,
                                    AllowanceAmount = null,
                                    NetworkIndicator = "IN",
                                    EffectiveDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                                    TerminationDate = null,
                                    AdditionalInfo = null
                                }
                            },
                            Payloads = new List<EligibilityPayloadEmbedded>()
                        }
                    },
                    CreatedAtUtc = new DateTime(2025, 3, 20, 16, 0, 0, DateTimeKind.Utc),
                    CreatedByUserId = "user_frontdesk_03"
                },

                // Encounter 4 - Emily, routine vision
                new EncounterDocument
                {
                    Id = "enc_101",
                    EncounterId = "enc_101",
                    Type = "encounter",
                    TenantId = "ten_xyz",
                    PracticeId = "prac_101",
                    LocationId = "loc_101",
                    PatientId = "pat_555",
                    VisitDate = new DateTime(2025, 3, 22, 0, 0, 0, DateTimeKind.Utc),
                    VisitType = "RoutineVision",
                    ExternalRef = "appt_555",
                    CoverageDecision = new CoverageDecisionEmbedded
                    {
                        EncounterCoverageDecisionId = "cob_dec_101",
                        PrimaryCoverageEnrollmentId = "cov_vision_555",
                        SecondaryCoverageEnrollmentId = null,
                        CobReason = "SingleCoverage_PrimaryVision",
                        OverriddenByUser = false,
                        OverrideNote = null,
                        CreatedAtUtc = new DateTime(2025, 3, 22, 13, 1, 0, DateTimeKind.Utc),
                        CreatedByUserId = "user_frontdesk_11"
                    },
                    EligibilityChecks = new List<EligibilityCheckEmbedded>
                    {
                        new EligibilityCheckEmbedded
                        {
                            EligibilityCheckId = "elig_101",
                            CoverageEnrollmentId = "cov_vision_555",
                            PayerId = "payer_vsp_001",
                            DateOfService = new DateTime(2025, 3, 22, 0, 0, 0, DateTimeKind.Utc),
                            RequestedAtUtc = new DateTime(2025, 3, 22, 13, 2, 0, DateTimeKind.Utc),
                            CompletedAtUtc = new DateTime(2025, 3, 22, 13, 2, 2, DateTimeKind.Utc),
                            Status = "Succeeded",
                            RawStatusCode = "1",
                            RawStatusDescription = "Active Coverage",
                            MemberIdSnapshot = "VSP55555",
                            GroupNumberSnapshot = "GRP555",
                            PlanNameSnapshot = "VSP Standard",
                            EffectiveDateSnapshot = new DateTime(2023, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                            TerminationDateSnapshot = null,
                            ErrorMessage = null,
                            CoverageLines = new List<CoverageLineEmbedded>
                            {
                                new CoverageLineEmbedded
                                {
                                    ServiceTypeCode = "47",
                                    CoverageDescription = "Vision Exam",
                                    CopayAmount = 15m,
                                    CoinsurancePercent = null,
                                    DeductibleAmount = null,
                                    RemainingDeductible = null,
                                    OutOfPocketMax = null,
                                    RemainingOutOfPocket = null,
                                    AllowanceAmount = null,
                                    NetworkIndicator = "IN",
                                    EffectiveDate = new DateTime(2023, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                                    TerminationDate = null,
                                    AdditionalInfo = "Annual exam covered"
                                }
                            },
                            Payloads = new List<EligibilityPayloadEmbedded>()
                        }
                    },
                    CreatedAtUtc = new DateTime(2025, 3, 22, 13, 0, 0, DateTimeKind.Utc),
                    CreatedByUserId = "user_frontdesk_11"
                }
    };

#endregion


