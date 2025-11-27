using System;
using System.Collections.Generic;

namespace Ec.Api.Contracts
{
    // 1. SESSION / CONTEXT

    public record UserSessionContextDto
    {
        public string UserId { get; init; } = default!;
        public string DisplayName { get; init; } = default!;
        public string TenantId { get; init; } = default!;
        public string TenantName { get; init; } = default!;
        public List<PracticeContextDto> Practices { get; init; } = new();
        public List<string> Roles { get; init; } = new();
    }

    public record PracticeContextDto
    {
        public string PracticeId { get; init; } = default!;
        public string Name { get; init; } = default!;
        public List<LocationContextDto> Locations { get; init; } = new();
    }

    public record LocationContextDto
    {
        public string LocationId { get; init; } = default!;
        public string Name { get; init; } = default!;
    }


    // 2. PRACTICES

    public record PracticeSummaryDto
    {
        public string PracticeId { get; init; } = default!;
        public string Name { get; init; } = default!;
        public bool IsActive { get; init; }
        public List<LocationSummaryDto> Locations { get; init; } = new();
    }

    public record LocationSummaryDto
    {
        public string LocationId { get; init; } = default!;
        public string Name { get; init; } = default!;
    }

    public record PracticeDetailDto : PracticeSummaryDto
    {
        public string? Phone { get; init; }
        public string? Email { get; init; }
    }


    // 3. PAYERS

    public record PayerDto
    {
        public string PayerId { get; init; } = default!;
        public string Name { get; init; } = default!;
        public string PlanType { get; init; } = default!;
        public string? AvailityPayerCode { get; init; }
        public string? X12PayerId { get; init; }
        public bool IsMedicare { get; init; }
        public bool IsMedicaid { get; init; }
    }


    // 4. PATIENTS & COVERAGE

    public record PatientSearchRequestDto
    {
        public string? PracticeId { get; init; }
        public string? LastName { get; init; }
        public string? FirstName { get; init; }
        public DateTime? DateOfBirth { get; init; }
        public string? MemberId { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }

    public record PatientSearchResultDto
    {
        public string PatientId { get; init; } = default!;
        public string PracticeId { get; init; } = default!;
        public string FirstName { get; init; } = default!;
        public string LastName { get; init; } = default!;
        public DateTime? DateOfBirth { get; init; }
        public string? PrimaryMemberId { get; init; }
        public string? PrimaryPayerName { get; init; }
    }

    public record PatientDetailDto
    {
        public string PatientId { get; init; } = default!;
        public string TenantId { get; init; } = default!;
        public string PracticeId { get; init; } = default!;
        public string FirstName { get; init; } = default!;
        public string LastName { get; init; } = default!;
        public DateTime? DateOfBirth { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }

        public List<CoverageEnrollmentDto> CoverageEnrollments { get; init; } = new();
        public List<EncounterSummaryDto> RecentEncounters { get; init; } = new();
    }

    public record PatientCreateRequestDto
    {
        public string PracticeId { get; init; } = default!;
        public string FirstName { get; init; } = default!;
        public string LastName { get; init; } = default!;
        public DateTime? DateOfBirth { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public List<CoverageEnrollmentCreateRequestDto> InitialCoverage { get; init; } = new();
    }

    public record PatientUpdateRequestDto
    {
        public string? PracticeId { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public DateTime? DateOfBirth { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
    }

    public record CoverageEnrollmentDto
    {
        public string CoverageEnrollmentId { get; init; } = default!;
        public string PayerId { get; init; } = default!;
        public string PlanType { get; init; } = default!;
        public string MemberId { get; init; } = default!;
        public string? GroupNumber { get; init; }
        public string RelationshipToSubscriber { get; init; } = default!;
        public string? SubscriberFirstName { get; init; }
        public string? SubscriberLastName { get; init; }
        public DateTime? SubscriberDob { get; init; }
        public bool IsEmployerPlan { get; init; }
        public bool IsVisionPlan { get; init; }
        public bool IsMedicalPlan { get; init; }
        public DateTime? EffectiveDate { get; init; }
        public DateTime? TerminationDate { get; init; }
        public bool IsActive { get; init; }
        public byte? CobPriorityHint { get; init; }
        public bool IsCobLocked { get; init; }
        public string? CobNotes { get; init; }
    }

    public record CoverageEnrollmentCreateRequestDto
    {
        public string PayerId { get; init; } = default!;
        public string PlanType { get; init; } = default!;
        public string MemberId { get; init; } = default!;
        public string? GroupNumber { get; init; }
        public string RelationshipToSubscriber { get; init; } = default!;
        public string? SubscriberFirstName { get; init; }
        public string? SubscriberLastName { get; init; }
        public DateTime? SubscriberDob { get; init; }
        public bool IsEmployerPlan { get; init; }
        public bool IsVisionPlan { get; init; }
        public bool IsMedicalPlan { get; init; }
        public DateTime? EffectiveDate { get; init; }
        public DateTime? TerminationDate { get; init; }
        public byte? CobPriorityHint { get; init; }
        public string? CobNotes { get; init; }
    }

    public record CoverageEnrollmentUpdateRequestDto
    {
        public string? MemberId { get; init; }
        public string? GroupNumber { get; init; }
        public bool? IsActive { get; init; }
        public byte? CobPriorityHint { get; init; }
        public bool? IsCobLocked { get; init; }
        public string? CobNotes { get; init; }
        public DateTime? EffectiveDate { get; init; }
        public DateTime? TerminationDate { get; init; }
    }


    // 5. ENCOUNTERS & COB

    public record EncounterCreateRequestDto
    {
        public string PatientId { get; init; } = default!;
        public string PracticeId { get; init; } = default!;
        public string LocationId { get; init; } = default!;
        public DateTime VisitDate { get; init; }
        public string VisitType { get; init; } = default!;
        public string? ExternalRef { get; init; }
    }

    public record EncounterUpdateRequestDto
    {
        public string? PracticeId { get; init; }
        public string? LocationId { get; init; }
        public DateTime? VisitDate { get; init; }
        public string? VisitType { get; init; }
        public string? ExternalRef { get; init; }
    }

    public record CoverageDecisionUpdateRequestDto
    {
        public string PrimaryCoverageEnrollmentId { get; init; } = default!;
        public string? SecondaryCoverageEnrollmentId { get; init; }
        public string CobReason { get; init; } = default!;
        public bool OverriddenByUser { get; init; }
        public string? OverrideNote { get; init; }
    }

    public record EligibilityCheckRequestDto
    {
        public string CoverageEnrollmentId { get; init; } = default!;
        public DateTime? OverrideDateOfService { get; init; }
        public List<string>? ServiceTypeCodes { get; init; }
        public bool ForceRefresh { get; init; }
    }

    public record EncounterSearchRequestDto
    {
        public string PracticeId { get; init; } = default!;
        public string? LocationId { get; init; }
        public DateTime FromDate { get; init; }
        public DateTime ToDate { get; init; }
        public string? VisitType { get; init; }
        public bool? HasEligibilityCheck { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 50;
    }

    public record PatientEncounterSearchRequestDto
    {
        public DateTime? FromDate { get; init; }
        public DateTime? ToDate { get; init; }
        public string? VisitType { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }

    public record EncounterSummaryDto
    {
        public string EncounterId { get; init; } = default!;
        public string PatientId { get; init; } = default!;
        public string PracticeId { get; init; } = default!;
        public string LocationId { get; init; } = default!;
        public DateTime VisitDate { get; init; }
        public string VisitType { get; init; } = default!;
        public bool HasEligibilityChecks { get; init; }
        public string? PrimaryPayerName { get; init; }
        public string? CobSummary { get; init; }
    }

    public record EncounterDetailDto : EncounterSummaryDto
    {
        public CoverageDecisionDto? CoverageDecision { get; init; }
        public List<EligibilityCheckDto> EligibilityChecks { get; init; } = new();
    }

    public record CoverageDecisionDto
    {
        public string EncounterCoverageDecisionId { get; init; } = default!;
        public string PrimaryCoverageEnrollmentId { get; init; } = default!;
        public string? SecondaryCoverageEnrollmentId { get; init; }
        public string CobReason { get; init; } = default!;
        public bool OverriddenByUser { get; init; }
        public string? OverrideNote { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public string? CreatedByUserId { get; init; }
    }

    public record EligibilityCheckSummaryDto
    {
        public string EligibilityCheckId { get; init; } = default!;
        public string CoverageEnrollmentId { get; init; } = default!;
        public string PayerId { get; init; } = default!;
        public DateTime DateOfService { get; init; }
        public DateTime RequestedAtUtc { get; init; }
        public DateTime? CompletedAtUtc { get; init; }
        public string Status { get; init; } = default!;
        public string? PayerName { get; init; }
    }

    public record EligibilityCheckDto : EligibilityCheckSummaryDto
    {
        public string? RawStatusCode { get; init; }
        public string? RawStatusDescription { get; init; }
        public string MemberIdSnapshot { get; init; } = default!;
        public string? GroupNumberSnapshot { get; init; }
        public string? PlanNameSnapshot { get; init; }
        public DateTime? EffectiveDateSnapshot { get; init; }
        public DateTime? TerminationDateSnapshot { get; init; }
        public string? ErrorMessage { get; init; }
        public List<CoverageLineDto> CoverageLines { get; init; } = new();
    }

    public record CoverageLineDto
    {
        public string ServiceTypeCode { get; init; } = default!;
        public string? CoverageDescription { get; init; }
        public decimal? CopayAmount { get; init; }
        public decimal? CoinsurancePercent { get; init; }
        public decimal? DeductibleAmount { get; init; }
        public decimal? RemainingDeductible { get; init; }
        public decimal? OutOfPocketMax { get; init; }
        public decimal? RemainingOutOfPocket { get; init; }
        public decimal? AllowanceAmount { get; init; }
        public string? NetworkIndicator { get; init; }
        public DateTime? EffectiveDate { get; init; }
        public DateTime? TerminationDate { get; init; }
        public string? AdditionalInfo { get; init; }
    }


    // 6. LOOKUPS

    public record LookupItemDto
    {
        public string Code { get; init; } = default!;
        public string DisplayName { get; init; } = default!;
    }

    public record VisitTypeServiceTypesDto
    {
        public string VisitType { get; init; } = default!;
        public List<string> ServiceTypeCodes { get; init; } = new();
    }


    // 7. CONFIG / ADMIN

    public record TenantConfigDto
    {
        public string TenantId { get; init; } = default!;
        public List<string> DefaultVisionServiceTypeCodes { get; init; } = new();
        public List<string> DefaultMedicalServiceTypeCodes { get; init; } = new();
        public int EligibilityTimeoutSeconds { get; init; }
    }

    public record TenantConfigUpdateRequestDto
    {
        public List<string>? DefaultVisionServiceTypeCodes { get; init; }
        public List<string>? DefaultMedicalServiceTypeCodes { get; init; }
        public int? EligibilityTimeoutSeconds { get; init; }
    }

    public record PayerConfigDto
    {
        public string PayerId { get; init; } = default!;
        public string? DisplayNameOverride { get; init; }
        public bool IsEnabled { get; init; }
        public string? Notes { get; init; }
    }

    public record PayerConfigUpdateRequestDto
    {
        public string? DisplayNameOverride { get; init; }
        public bool? IsEnabled { get; init; }
        public string? Notes { get; init; }
    }


    // 8. GENERIC PAGED RESULT

    public record PagedResult<T>
    {
        public int Page { get; init; }
        public int PageSize { get; init; }
        public long TotalCount { get; init; }
        public List<T> Items { get; init; } = new();
    }
}
