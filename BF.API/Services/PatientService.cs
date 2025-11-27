using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ec.Api.Contracts;

namespace Ec.Api.Services
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _patientRepo;
        private readonly ICoverageRepository _coverageRepo;
        private readonly IEncounterRepository _encounterRepo;

        public PatientService(
            IPatientRepository patientRepo,
            ICoverageRepository coverageRepo,
            IEncounterRepository encounterRepo)
        {
            _patientRepo = patientRepo;
            _coverageRepo = coverageRepo;
            _encounterRepo = encounterRepo;
        }

        public async Task<PagedResult<PatientSearchResultDto>> SearchPatientsAsync(
            string tenantId,
            PatientSearchRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            if (request.Page <= 0 || request.PageSize <= 0)
                throw new ArgumentException("Invalid paging parameters.", nameof(request));

            var result = await _patientRepo.SearchAsync(tenantId, request);
            return result; // assuming repo returns directly mapped PagedResult<PatientSearchResultDto>
        }

        public async Task<PatientDetailDto> GetPatientAsync(string tenantId, string patientId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            if (string.IsNullOrWhiteSpace(patientId))
                throw new ArgumentException("patientId is required.", nameof(patientId));

            var entity = await _patientRepo.GetByIdAsync(tenantId, patientId);
            if (entity is null)
                throw new KeyNotFoundException("Patient not found.");

            return MapToDetailDto(entity);
        }

        public async Task<PatientDetailDto> CreatePatientAsync(string tenantId, PatientCreateRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
                throw new ArgumentException("First and last name are required.", nameof(request));

            var entity = new PatientEntity
            {
                TenantId = tenantId,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                DateOfBirth = request.DateOfBirth,
                Email = request.Email,
                Phone = request.Phone
            };

            await _patientRepo.CreateAsync(entity);
            return MapToDetailDto(entity);
        }

        public async Task<PatientDetailDto> UpdatePatientAsync(
            string tenantId,
            string patientId,
            PatientUpdateRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            if (string.IsNullOrWhiteSpace(patientId))
                throw new ArgumentException("patientId is required.", nameof(patientId));

            var entity = await _patientRepo.GetByIdAsync(tenantId, patientId);
            if (entity is null)
                throw new KeyNotFoundException("Patient not found.");

            if (request.FirstName is not null) entity.FirstName = request.FirstName.Trim();
            if (request.LastName is not null) entity.LastName = request.LastName.Trim();
            if (request.DateOfBirth.HasValue) entity.DateOfBirth = request.DateOfBirth;
            if (request.Email is not null) entity.Email = request.Email;
            if (request.Phone is not null) entity.Phone = request.Phone;

            await _patientRepo.UpdateAsync(entity);
            return MapToDetailDto(entity);
        }

        public async Task<CoverageEnrollmentDto> AddCoverageEnrollmentAsync(
            string tenantId,
            string patientId,
            CoverageEnrollmentCreateRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(patientId))
                throw new ArgumentException("patientId is required.", nameof(patientId));

            var patient = await _patientRepo.GetByIdAsync(tenantId, patientId);
            if (patient is null)
                throw new KeyNotFoundException("Patient not found.");

            var entity = new CoverageEnrollmentEntity
            {
                TenantId = tenantId,
                PatientId = patientId,
                PayerId = request.PayerId,
                MemberId = request.MemberId,
                GroupNumber = request.GroupNumber,
                CoverageType = request.CoverageType,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = request.EffectiveTo
            };

            await _coverageRepo.CreateAsync(entity);
            return MapToCoverageDto(entity);
        }

        public async Task<CoverageEnrollmentDto> UpdateCoverageEnrollmentAsync(
            string tenantId,
            string patientId,
            string coverageEnrollmentId,
            CoverageEnrollmentUpdateRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(patientId))
                throw new ArgumentException("patientId is required.", nameof(patientId));
            if (string.IsNullOrWhiteSpace(coverageEnrollmentId))
                throw new ArgumentException("coverageEnrollmentId is required.", nameof(coverageEnrollmentId));

            var entity = await _coverageRepo.GetByIdAsync(tenantId, patientId, coverageEnrollmentId);
            if (entity is null)
                throw new KeyNotFoundException("Coverage enrollment not found.");

            if (request.MemberId is not null) entity.MemberId = request.MemberId;
            if (request.GroupNumber is not null) entity.GroupNumber = request.GroupNumber;
            if (request.CoverageType is not null) entity.CoverageType = request.CoverageType;
            if (request.EffectiveFrom.HasValue) entity.EffectiveFrom = request.EffectiveFrom;
            if (request.EffectiveTo.HasValue) entity.EffectiveTo = request.EffectiveTo;

            await _coverageRepo.UpdateAsync(entity);
            return MapToCoverageDto(entity);
        }

        public async Task DeleteCoverageEnrollmentAsync(
            string tenantId,
            string patientId,
            string coverageEnrollmentId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(patientId))
                throw new ArgumentException("patientId is required.", nameof(patientId));
            if (string.IsNullOrWhiteSpace(coverageEnrollmentId))
                throw new ArgumentException("coverageEnrollmentId is required.", nameof(coverageEnrollmentId));

            var entity = await _coverageRepo.GetByIdAsync(tenantId, patientId, coverageEnrollmentId);
            if (entity is null)
                throw new KeyNotFoundException("Coverage enrollment not found.");

            await _coverageRepo.DeleteAsync(entity);
        }

        public async Task<PagedResult<EncounterSummaryDto>> GetPatientEncountersAsync(
            string tenantId,
            string patientId,
            PatientEncounterSearchRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(patientId))
                throw new ArgumentException("patientId is required.", nameof(patientId));

            return await _encounterRepo.SearchForPatientAsync(tenantId, patientId, request);
        }

        private static PatientDetailDto MapToDetailDto(PatientEntity e)
        {
            return new PatientDetailDto
            {
                PatientId = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                DateOfBirth = e.DateOfBirth,
                Email = e.Email,
                Phone = e.Phone
                // add coverage, notes, etc. as needed
            };
        }

        private static CoverageEnrollmentDto MapToCoverageDto(CoverageEnrollmentEntity e)
        {
            return new CoverageEnrollmentDto
            {
                CoverageEnrollmentId = e.Id,
                PayerId = e.PayerId,
                MemberId = e.MemberId,
                GroupNumber = e.GroupNumber,
                CoverageType = e.CoverageType,
                EffectiveFrom = e.EffectiveFrom,
                EffectiveTo = e.EffectiveTo
            };
        }
    }
}
