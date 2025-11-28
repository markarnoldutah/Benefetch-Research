using System.Collections.Generic;

namespace BF.Auth0.Seed;

public class EcPermission
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
}

public class EcRole
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public List<string> Permissions { get; set; } = new();
}

public static class EcSeedData
{
    public static IReadOnlyList<EcPermission> Permissions => new[]
    {
        new EcPermission { Name = "practice:read",  Description = "View practice details and locations" },
        new EcPermission { Name = "payers:read",    Description = "View payer list and payer details" },
        new EcPermission { Name = "payers:write",   Description = "Manage payer configuration for the practice" },
        new EcPermission { Name = "patients:read",  Description = "Search and view patient records and details" },
        new EcPermission { Name = "patients:write", Description = "Create and update patient records" },
        new EcPermission { Name = "patients:delete", Description = "Soft delete patient records" },
        new EcPermission { Name = "coverage:read",  Description = "View coverage enrollments for patients" },
        new EcPermission { Name = "coverage:write", Description = "Add, update, and delete coverage enrollments" },
        new EcPermission { Name = "encounters:read",  Description = "View encounters and encounter summaries" },
        new EcPermission { Name = "encounters:write", Description = "Create and update encounters, including coverage decisions" },
        new EcPermission { Name = "eligibility:run",           Description = "Run eligibility checks against payers" },
        new EcPermission { Name = "eligibility:view-result",   Description = "View parsed eligibility and benefit summaries" },
        new EcPermission { Name = "eligibility:view-raw-response", Description = "View raw eligibility response payloads" },
        new EcPermission { Name = "lookups:read",  Description = "Read lookup data such as visit types and COB reasons" },
        new EcPermission { Name = "lookups:write", Description = "Customize practice-level lookup values" },
        new EcPermission { Name = "config:read",   Description = "View tenant-level EC configuration and payer config" },
        new EcPermission { Name = "config:write",  Description = "Manage tenant-level EC configuration and payer config" },
        new EcPermission { Name = "users:read",    Description = "View users within the tenant" },
        new EcPermission { Name = "users:assign-roles", Description = "Assign or remove EC roles for users in the tenant" },
        new EcPermission { Name = "reports:read",  Description = "View EC reports and analytics dashboards" },
        new EcPermission { Name = "reports:export",Description = "Export EC reporting data (e.g., CSV, Excel)" },
        new EcPermission { Name = "integrations:read",  Description = "View EHR/PM integrations and connection status" },
        new EcPermission { Name = "integrations:write", Description = "Configure and manage EHR/PM integrations" },
        new EcPermission { Name = "apikeys:manage",     Description = "Create and revoke API keys or webhooks for the tenant" }
    };

    public static IReadOnlyList<EcRole> Roles => new[]
    {
        new EcRole
        {
            Name = "frontdesk-basic",
            Description = "Front desk staff with core EC workflow access",
            Permissions =
            {
                "practice:read",
                "payers:read",
                "patients:read",
                "patients:write",
                "coverage:read",
                "coverage:write",
                "encounters:read",
                "encounters:write",
                "eligibility:run",
                "eligibility:view-result",
                "lookups:read"
            }
        },
        new EcRole
        {
            Name = "frontdesk-lead",
            Description = "Senior front desk with extended EC capabilities",
            Permissions =
            {
                "practice:read",
                "payers:read",
                "payers:write",
                "patients:read",
                "patients:write",
                "patients:delete",
                "coverage:read",
                "coverage:write",
                "encounters:read",
                "encounters:write",
                "eligibility:run",
                "eligibility:view-result",
                "eligibility:view-raw-response",
                "lookups:read",
                "lookups:write",
                "reports:read"
            }
        },
        new EcRole
        {
            Name = "provider",
            Description = "Optometrist/ophthalmologist using EC at chairside",
            Permissions =
            {
                "practice:read",
                "payers:read",
                "patients:read",
                "coverage:read",
                "encounters:read",
                "encounters:write",
                "eligibility:view-result",
                "lookups:read",
                "reports:read"
            }
        },
        new EcRole
        {
            Name = "billing",
            Description = "Billing and insurance specialist",
            Permissions =
            {
                "practice:read",
                "payers:read",
                "payers:write",
                "patients:read",
                "patients:write",
                "coverage:read",
                "coverage:write",
                "encounters:read",
                "encounters:write",
                "eligibility:run",
                "eligibility:view-result",
                "eligibility:view-raw-response",
                "config:read",
                "config:write",
                "lookups:read",
                "lookups:write",
                "reports:read",
                "reports:export",
                "integrations:read",
                "integrations:write"
            }
        },
        new EcRole
        {
            Name = "practice-admin",
            Description = "Full administrative control for an EC tenant",
            Permissions =
            {
                "practice:read",
                "payers:read",
                "payers:write",
                "patients:read",
                "patients:write",
                "patients:delete",
                "coverage:read",
                "coverage:write",
                "encounters:read",
                "encounters:write",
                "eligibility:run",
                "eligibility:view-result",
                "eligibility:view-raw-response",
                "lookups:read",
                "lookups:write",
                "config:read",
                "config:write",
                "users:read",
                "users:assign-roles",
                "reports:read",
                "reports:export",
                "integrations:read",
                "integrations:write",
                "apikeys:manage"
            }
        },
        new EcRole
        {
            Name = "auditor-readonly",
            Description = "Read-only access for audits and oversight",
            Permissions =
            {
                "practice:read",
                "payers:read",
                "patients:read",
                "coverage:read",
                "encounters:read",
                "eligibility:view-result",
                "lookups:read",
                "reports:read"
            }
        }
    };
}
