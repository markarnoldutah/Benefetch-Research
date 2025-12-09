private static async Task SeedAsync(CosmosClient client)
{
    var database = client.GetDatabase("bfdb");

    var tenantsContainer = database.GetContainer("tenants");
    var practicesContainer = database.GetContainer("practices");
    var patientsContainer = database.GetContainer("patients");
    var encountersContainer = database.GetContainer("encounters");
    var payersContainer = database.GetContainer("payers");
    var lookupsContainer = database.GetContainer("lookups");

    // ... existing seed calls for tenants/practices/patients/encounters/payers ...

    // Lookups
    var lookupSets = BuildLookups();

    foreach (var set in lookupSets)
    {
        await lookupsContainer.UpsertItemAsync(
            set,
            new PartitionKey(set.TenantId));
    }
}



private static IReadOnlyList<LookupSet> BuildLookups()
{
    var now = DateTime.UtcNow;

    return new List<LookupSet>
    {
        // 1. Sex / Gender
        new LookupSet
        {
            Id = "sex-gender",
            TenantId = "GLOBAL",
            Type = "lookupSet",
            Category = "SexGender",
            Name = "Sex / Gender Options",
            Description = "Used when entering or updating patient demographics.",
            OverrideMode = LookupOverrideMode.GlobalOnly,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Items = new List<LookupItem>
            {
                new() { Code = "M",     Name = "Male",               SortOrder = 1, IsActive = true },
                new() { Code = "F",     Name = "Female",             SortOrder = 2, IsActive = true },
                new() { Code = "X",     Name = "Other / Not listed", SortOrder = 3, IsActive = true }
            }
        },

        // 2. Relationship to Subscriber
        new LookupSet
        {
            Id = "relationship-to-subscriber",
            TenantId = "GLOBAL",
            Type = "lookupSet",
            Category = "Relationship",
            Name = "Relationship to Subscriber",
            Description = "Used when entering coverage enrollment details.",
            OverrideMode = LookupOverrideMode.GlobalOnly,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Items = new List<LookupItem>
            {
                new() { Code = "self",   Name = "Self",   SortOrder = 1, IsActive = true },
                new() { Code = "spouse", Name = "Spouse", SortOrder = 2, IsActive = true },
                new() { Code = "child",  Name = "Child",  SortOrder = 3, IsActive = true },
                new() { Code = "other",  Name = "Other",  SortOrder = 4, IsActive = true }
            }
        },

        // 3. U.S. States (shortened here; fill in the rest)
        new LookupSet
        {
            Id = "us-states",
            TenantId = "GLOBAL",
            Type = "lookupSet",
            Category = "Region",
            Name = "U.S. States",
            Description = "State choices for all address fields.",
            OverrideMode = LookupOverrideMode.GlobalOnly,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Items = new List<LookupItem>
            {
                new() { Code = "AL", Name = "Alabama",   SortOrder = 1,  IsActive = true },
                new() { Code = "AK", Name = "Alaska",    SortOrder = 2,  IsActive = true },
                // ... all states in order ...
                new() { Code = "UT", Name = "Utah",      SortOrder = 44, IsActive = true },
                new() { Code = "WY", Name = "Wyoming",   SortOrder = 50, IsActive = true }
            }
        },

        // 4. Coverage Types (Vision / Medical, Primary / Secondary)
        new LookupSet
        {
            Id = "coverage-types",
            TenantId = "GLOBAL",
            Type = "lookupSet",
            Category = "CoverageType",
            Name = "Coverage Types",
            Description = "Types of coverage that can be linked to a patient.",
            OverrideMode = LookupOverrideMode.GlobalOnly,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Items = new List<LookupItem>
            {
                new() { Code = "vision-primary",    Name = "Vision (Primary)",    SortOrder = 1, IsActive = true },
                new() { Code = "vision-secondary",  Name = "Vision (Secondary)",  SortOrder = 2, IsActive = true },
                new() { Code = "medical-primary",   Name = "Medical (Primary)",   SortOrder = 3, IsActive = true },
                new() { Code = "medical-secondary", Name = "Medical (Secondary)", SortOrder = 4, IsActive = true }
            }
        },

        // 5. Eligibility Status
        new LookupSet
        {
            Id = "eligibility-status",
            TenantId = "GLOBAL",
            Type = "lookupSet",
            Category = "EligibilityStatus",
            Name = "Eligibility Check Statuses",
            Description = "Status outcomes for an eligibility check.",
            OverrideMode = LookupOverrideMode.GlobalOnly,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Items = new List<LookupItem>
            {
                new() { Code = "pending",   Name = "Pending",   SortOrder = 1, IsActive = true },
                new() { Code = "succeeded", Name = "Succeeded", SortOrder = 2, IsActive = true },
                new() { Code = "failed",    Name = "Failed",    SortOrder = 3, IsActive = true },
                new() { Code = "not-found", Name = "Not Found", SortOrder = 4, IsActive = true }
            }
        },

        // 6. COB Rules
        new LookupSet
        {
            Id = "cob-rules",
            TenantId = "GLOBAL",
            Type = "lookupSet",
            Category = "CobRules",
            Name = "Coordination of Benefits Rules",
            Description = "Defines how EC determines primary/secondary plans.",
            OverrideMode = LookupOverrideMode.GlobalOnly,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Items = new List<LookupItem>
            {
                new() { Code = "vision-primary-routine",  Name = "Vision first for routine exams", SortOrder = 1, IsActive = true },
                new() { Code = "medical-primary-medical", Name = "Medical first for medical visits", SortOrder = 2, IsActive = true },
                new() { Code = "birthday-rule",           Name = "Birthday Rule",                  SortOrder = 3, IsActive = true },
                new() { Code = "payer-specified",         Name = "Payer-specified order",          SortOrder = 4, IsActive = true }
            }
        },

        // 7. Visit Types
        new LookupSet
        {
            Id = "visit-types",
            TenantId = "GLOBAL",
            Type = "lookupSet",
            Category = "VisitType",
            Name = "Visit Types",
            Description = "Displayed when creating an encounter.",
            OverrideMode = LookupOverrideMode.GlobalOnly,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Items = new List<LookupItem>
            {
                new() { Code = "routine", Name = "Routine Eye Exam",         SortOrder = 1, IsActive = true },
                new() { Code = "medical", Name = "Medical Eye Visit",        SortOrder = 2, IsActive = true },
                new() { Code = "cl-fit",  Name = "Contact Lens Evaluation",  SortOrder = 3, IsActive = true }
            }
        },

        // 8. Visit Reasons
        new LookupSet
        {
            Id = "visit-reasons",
            TenantId = "GLOBAL",
            Type = "lookupSet",
            Category = "VisitReason",
            Name = "Visit Reasons",
            Description = "Shown when user selects a reason for the visit.",
            OverrideMode = LookupOverrideMode.GlobalOnly,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Items = new List<LookupItem>
            {
                new() { Code = "annual",   Name = "Annual Exam",         SortOrder = 1, IsActive = true },
                new() { Code = "diabetic", Name = "Diabetic Screening",  SortOrder = 2, IsActive = true },
                new() { Code = "peds",     Name = "Pediatric Screening", SortOrder = 3, IsActive = true },
                new() { Code = "problem",  Name = "Problem Visit",       SortOrder = 4, IsActive = true }
            }
        }
    };
}
