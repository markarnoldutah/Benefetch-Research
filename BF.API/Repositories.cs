using System.Collections.Generic;
using System.Threading.Tasks;
using Ec.Api.Contracts;

namespace Ec.Api.Persistence
{
    // =====================================================
    // Shared: Paged result (if not already defined)
    // =====================================================
    //
    // If you already have this in Ec.Api.Contracts, reuse that.
    // Included here just as a reminder of the shape you’ll want.

    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = new List<T>();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int? TotalCount { get; init; }   // optional for Cosmos
        public string? ContinuationToken { get; init; }
    }

    // =====================================================
    // Entities (sketch / placeholders)
    // =====================================================
    //
    // These should mirror your Cosmos document shapes:
    // - PartitionKey typically = tenantId
    // - id = entity Id (e.g., patientId, encounterId)

    public class PracticeEntity { public string Id { get; set; } = default!; public string TenantId { get; set; } = default!; /* ... */ }
    public class PayerEntity { public string Id { get; set; } = default!; public string TenantId { get; set; } = default!; /* ... */ }
    public class PatientEntity { public string Id { get; set; } = default!; public string TenantId { get; set; } = default!; /* ... */ }
    public class CoverageEnrollmentEntity { public string Id { get; set; } = default!; public string TenantId { get; set; } = default!; public string PatientId { get; set; } = default!; /* ... */ }
    public class EncounterEntity { public string Id { get; set; } = default!; public string TenantId { get; set; } = default!; public string PatientId { get; set; } = default!; /* ... */ }
    public class EligibilityCheckEntity { public string Id { get; set; } = default!; public string TenantId { get; set; } = default!; public string EncounterId { get; set; } = default!; /* ... */ }

    public class TenantConfigEntity { public string TenantId { get; set; } = default!; /* ... */ }
    public class PayerConfigEntity { public string TenantId { get; set; } = default!; public string PayerId { get; set; } = default!; /* ... */ }

    // =====================================================
    // Practices
    // =====================================================

    public interface IPracticeRepository
    {
        /// <summary>
        /// Returns all practices for a tenant. includeLocations can drive projection or joins.
        /// </summary>
        Task<List<PracticeEntity>> GetPracticesForTenantAsync(string tenantId, bool includeLocations);

        /// <summary>
        /// Returns a single practice or null if not found.
        /// Service layer will throw KeyNotFoundException.
        /// </summary>
        Task<PracticeEntity?> GetByIdAsync(string tenantId, string practiceId);
    }

    // =====================================================
    // Payers
    // =====================================================

    public interface IPayerRepository
    {
        /// <summary>
        /// Cosmos-style search; returns zero-or-more payer entities for the tenant.
        /// </summary>
        Task<List<PayerEntity>> SearchAsync(string tenantId, string? planType, string? search);

        /// <summary>
        /// Returns a single payer or null if not found.
        /// </summary>
        Task<PayerEntity?> GetByIdAsync(string tenantId, string payerId);
    }

    // =====================================================
    // Patients
    // =====================================================

    public interface IPatientRepository
    {
        /// <summary>
        /// Paged search for patients in a tenant.
        /// For Cosmos, Page/PageSize + ContinuationToken map to MaxItemCount + continuation.
        /// </summary>
        Task<PagedResult<PatientSearchResultDto>> SearchAsync(
            string tenantId,
            PatientSearchRequestDto request);

        /// <summary>
        /// Returns a single patient entity or null.
        /// </summary>
        Task<PatientEntity?> GetByIdAsync(string tenantId, string patientId);

        /// <summary>
        /// Creates a new patient entity (id should be assigned by caller or here).
        /// </summary>
        Task CreateAsync(PatientEntity entity);

        /// <summary>
        /// Updates an existing patient entity (ETag / concurrency handled inside).
        /// </summary>
        Task UpdateAsync(PatientEntity entity);
    }

    // =====================================================
    // Coverage (CoverageEnrollments)
    // =====================================================

    public interface ICoverageRepository
    {
        /// <summary>
        /// Returns a coverage enrollment by tenant + patient + coverageEnrollmentId
        /// or null if it doesn't exist.
        /// Partition can still be tenantId; patientId can be in id or secondary field.
        /// </summary>
        Task<CoverageEnrollmentEntity?> GetByIdAsync(
            string tenantId,
            string patientId,
            string coverageEnrollmentId);

        Task CreateAsync(CoverageEnrollmentEntity entity);

        Task UpdateAsync(CoverageEnrollmentEntity entity);

        Task DeleteAsync(CoverageEnrollmentEntity entity);
    }

    // =====================================================
    // Encounters + Eligibility Checks
    // =====================================================

    public interface IEncounterRepository
    {
        /// <summary>
        /// Returns encounter by tenant + encounterId or null.
        /// </summary>
        Task<EncounterEntity?> GetByIdAsync(string tenantId, string encounterId);

        Task CreateAsync(EncounterEntity entity);

        Task UpdateAsync(EncounterEntity entity);

        /// <summary>
        /// Search encounters for a specific patient, paged.
        /// Likely filtered on patientId + date ranges.
        /// </summary>
        Task<PagedResult<EncounterSummaryDto>> SearchForPatientAsync(
            string tenantId,
            string patientId,
            PatientEncounterSearchRequestDto request);

        /// <summary>
        /// Search encounters across a tenant, paged.
        /// </summary>
        Task<PagedResult<EncounterSummaryDto>> SearchAsync(
            string tenantId,
            EncounterSearchRequestDto request);

        // ---------- Eligibility Checks ----------

        /// <summary>
        /// Creates an eligibility check record attached to an encounter.
        /// </summary>
        Task<EligibilityCheckEntity> CreateEligibilityCheckAsync(
            string tenantId,
            string encounterId,
            EligibilityCheckRequestDto request);

        /// <summary>
        /// Returns a list of eligibility check summaries for an encounter.
        /// </summary>
        Task<List<EligibilityCheckSummaryDto>> GetEligibilityChecksAsync(
            string tenantId,
            string encounterId);

        /// <summary>
        /// Returns a specific eligibility check or null.
        /// </summary>
        Task<EligibilityCheckEntity?> GetEligibilityCheckAsync(
            string tenantId,
            string encounterId,
            string eligibilityCheckId);
    }

    // =====================================================
    // Lookups
    // =====================================================

    public interface ILookupRepository
    {
        /// <summary>
        /// Returns visit-type lookup items (static or tenant-specific).
        /// </summary>
        Task<List<LookupItemDto>> GetVisitTypesAsync();

        /// <summary>
        /// Returns COB reason lookup items.
        /// </summary>
        Task<List<LookupItemDto>> GetCobReasonsAsync();

        /// <summary>
        /// Returns mapping of visit types to default service types.
        /// </summary>
        Task<List<VisitTypeServiceTypesDto>> GetVisitTypeServiceTypesAsync();
    }

    // =====================================================
    // Config
    // =====================================================

    public interface IConfigRepository
    {
        /// <summary>
        /// Returns tenant configuration entity or null.
        /// </summary>
        Task<TenantConfigEntity?> GetTenantConfigAsync(string tenantId);

        /// <summary>
        /// Creates/updates tenant configuration entity.
        /// </summary>
        Task SaveTenantConfigAsync(TenantConfigEntity entity);

        /// <summary>
        /// Returns all payer config DTOs for a tenant.
        /// (You can use a pure DTO here since it’s read-mostly config.)
        /// </summary>
        Task<List<PayerConfigDto>> GetPayerConfigsAsync(string tenantId);

        /// <summary>
        /// Returns a single payer config for a tenant or null.
        /// </summary>
        Task<PayerConfigDto?> GetPayerConfigAsync(string tenantId, string payerId);

        /// <summary>
        /// Saves a payer config for a tenant.
        /// </summary>
        Task SavePayerConfigAsync(PayerConfigDto config);
    }

    // =====================================================
    // Tenant access (for SessionService, etc.)
    // =====================================================

    public interface ITenantAccessService
    {
        Task<bool> HasAccessAsync(string userId, string tenantId);

        /// <summary>
        /// Returns roles/claims for session context; implementation
        /// can read from identity provider claims, DB, or a mix.
        /// </summary>
        IReadOnlyList<string> GetRolesForUser(ClaimsPrincipal user);
    }
}
