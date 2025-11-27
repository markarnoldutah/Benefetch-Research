using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ec.Api.Contracts;

namespace Ec.Api.Services
{
    public class EncounterService : IEncounterService
    {
        private readonly IEncounterRepository _encounterRepo;
        private readonly IPatientRepository _patientRepo;
        private readonly ICoverageRepository _coverageRepo;

        public EncounterService(
            IEncounterRepository encounterRepo,
            IPatientRepository patientRepo,
            ICoverageRepository coverageRepo)
        {
            _encounterRepo = encounterRepo;
            _patientRepo = patientRepo;
            _coverageRepo = coverageRepo;
        }

        public async Task<EncounterDetailDto> CreateEncounterAsync(
            string tenantId,
            EncounterCreateRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            if (string.IsNullOrWhiteSpace(request.PatientId))
                throw new ArgumentException("PatientId is required.", nameof(request));

            // Ensure patient exists
            var patient = await _patientRepo.GetByIdAsync(tenantId, request.PatientId);
            if (patient is null)
                throw new KeyNotFoundException("Patient not found.");

            var entity = new EncounterEntity
            {
                TenantId = tenantId,
                PatientId = request.PatientId,
                PracticeId = request.PracticeId,
                LocationId = request.LocationId,
                VisitDate = request.VisitDate,
                VisitTypeCode = request.VisitTypeCode,
                ReasonForVisit = request.ReasonForVisit
            };

            await _encounterRepo.CreateAsync(entity);
            return MapToDetailDto(entity);
        }

        public async Task<EncounterDetailDto> GetEncounterAsync(string tenantId, string encounterId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(encounterId))
                throw new ArgumentException("encounterId is required.", nameof(encounterId));

            var entity = await _encounterRepo.GetByIdAsync(tenantId, encounterId);
            if (entity is null)
                throw new KeyNotFoundException("Encounter not found.");

            return MapToDetailDto(entity);
        }

        public async Task<EncounterDetailDto> UpdateEncounterAsync(
            string tenantId,
            string encounterId,
            EncounterUpdateRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(encounterId))
                throw new ArgumentException("encounterId is required.", nameof(encounterId));

            var entity = await _encounterRepo.GetByIdAsync(tenantId, encounterId);
            if (entity is null)
                throw new KeyNotFoundException("Encounter not found.");

            if (request.VisitDate.HasValue) entity.VisitDate = request.VisitDate;
            if (request.VisitTypeCode is not null) entity.VisitTypeCode = request.VisitTypeCode;
            if (request.ReasonForVisit is not null) entity.ReasonForVisit = request.ReasonForVisit;

            await _encounterRepo.UpdateAsync(entity);
            return MapToDetailDto(entity);
        }

        public async Task<CoverageDecisionDto> SetCoverageDecisionAsync(
            string tenantId,
            string encounterId,
            CoverageDecisionUpdateRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(encounterId))
                throw new ArgumentException("encounterId is required.", nameof(encounterId));

            var encounter = await _encounterRepo.GetByIdAsync(tenantId, encounterId);
            if (encounter is null)
                throw new KeyNotFoundException("Encounter not found.");

            // ensure coverage exists if using coverageEnrollmentId
            if (!string.IsNullOrWhiteSpace(request.PrimaryCoverageEnrollmentId))
            {
                var coverage = await _coverageRepo.GetByIdAsync(
                    tenantId,
                    encounter.PatientId,
                    request.PrimaryCoverageEnrollmentId);

                if (coverage is null)
                    throw new KeyNotFoundException("Coverage enrollment not found.");
            }

            encounter.CoverageDecision = new CoverageDecision
            {
                PrimaryCoverageEnrollmentId = request.PrimaryCoverageEnrollmentId,
                SecondaryCoverageEnrollmentId = request.SecondaryCoverageEnrollmentId,
                CobReasonCode = request.CobReasonCode
            };

            await _encounterRepo.UpdateAsync(encounter);

            return new CoverageDecisionDto
            {
                PrimaryCoverageEnrollmentId = encounter.CoverageDecision.PrimaryCoverageEnrollmentId,
                SecondaryCoverageEnrollmentId = encounter.CoverageDecision.SecondaryCoverageEnrollmentId,
                CobReasonCode = encounter.CoverageDecision.CobReasonCode
            };
        }

        public async Task<EligibilityCheckDto> RunEligibilityCheckAsync(
            string tenantId,
            string encounterId,
            EligibilityCheckRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(encounterId))
                throw new ArgumentException("encounterId is required.", nameof(encounterId));

            var encounter = await _encounterRepo.GetByIdAsync(tenantId, encounterId);
            if (encounter is null)
                throw new KeyNotFoundException("Encounter not found.");

            // Validate the referenced coverage enrollment exists
            var coverage = await _coverageRepo.GetByIdAsync(
                tenantId,
                encounter.PatientId,
                request.CoverageEnrollmentId);

            if (coverage is null)
                throw new KeyNotFoundException("Coverage enrollment not found.");

            // Call Availity / clearinghouse here (omitted)
            var checkEntity = await _encounterRepo.CreateEligibilityCheckAsync(tenantId, encounterId, request);

            return MapToEligibilityDto(checkEntity);
        }

        public async Task<List<EligibilityCheckSummaryDto>> GetEligibilityChecksAsync(
            string tenantId,
            string encounterId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(encounterId))
                throw new ArgumentException("encounterId is required.", nameof(encounterId));

            return await _encounterRepo.GetEligibilityChecksAsync(tenantId, encounterId);
        }

        public async Task<EligibilityCheckDto> GetEligibilityCheckAsync(
            string tenantId,
            string encounterId,
            string eligibilityCheckId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(encounterId))
                throw new ArgumentException("encounterId is required.", nameof(encounterId));
            if (string.IsNullOrWhiteSpace(eligibilityCheckId))
                throw new ArgumentException("eligibilityCheckId is required.", nameof(eligibilityCheckId));

            var entity = await _encounterRepo.GetEligibilityCheckAsync(tenantId, encounterId, eligibilityCheckId);
            if (entity is null)
                throw new KeyNotFoundException("Eligibility check not found.");

            return MapToEligibilityDto(entity);
        }

        public async Task<PagedResult<EncounterSummaryDto>> SearchEncountersAsync(
            string tenantId,
            EncounterSearchRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            if (request.Page <= 0 || request.PageSize <= 0)
                throw new ArgumentException("Invalid paging parameters.", nameof(request));

            return await _encounterRepo.SearchAsync(tenantId, request);
        }

        private static EncounterDetailDto MapToDetailDto(EncounterEntity e)
        {
            return new EncounterDetailDto
            {
                EncounterId = e.Id,
                PatientId = e.PatientId,
                PracticeId = e.PracticeId,
                LocationId = e.LocationId,
                VisitDate = e.VisitDate,
                VisitTypeCode = e.VisitTypeCode,
                ReasonForVisit = e.ReasonForVisit
            };
        }

        private static EligibilityCheckDto MapToEligibilityDto(EligibilityCheckEntity e)
        {
            return new EligibilityCheckDto
            {
                EligibilityCheckId = e.Id,
                EncounterId = e.EncounterId,
                CoverageEnrollmentId = e.CoverageEnrollmentId,
                RequestTimestampUtc = e.RequestTimestampUtc,
                ResponseTimestampUtc = e.ResponseTimestampUtc,
                RawResponse = e.RawResponse,
                ParsedBenefits = e.ParsedBenefits
            };
        }
    }
}
