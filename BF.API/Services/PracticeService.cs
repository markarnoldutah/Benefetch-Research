using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ec.Api.Contracts;

namespace Ec.Api.Services
{
    public class PracticeService : IPracticeService
    {
        private readonly IPracticeRepository _repo;

        public PracticeService(IPracticeRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<PracticeSummaryDto>> GetPracticesAsync(string tenantId, bool includeLocations)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            var entities = await _repo.GetPracticesForTenantAsync(tenantId, includeLocations);
            return MapToSummaryDtos(entities);
        }

        public async Task<PracticeDetailDto> GetPracticeAsync(string tenantId, string practiceId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            if (string.IsNullOrWhiteSpace(practiceId))
                throw new ArgumentException("practiceId is required.", nameof(practiceId));

            var entity = await _repo.GetByIdAsync(tenantId, practiceId);
            if (entity is null)
                throw new KeyNotFoundException("Practice not found.");

            return MapToDetailDto(entity);
        }

        private static List<PracticeSummaryDto> MapToSummaryDtos(IEnumerable<PracticeEntity> entities)
        {
            var list = new List<PracticeSummaryDto>();
            foreach (var e in entities)
            {
                list.Add(new PracticeSummaryDto
                {
                    PracticeId = e.Id,
                    Name = e.Name,
                    IsActive = e.IsActive
                });
            }
            return list;
        }

        private static PracticeDetailDto MapToDetailDto(PracticeEntity e)
        {
            return new PracticeDetailDto
            {
                PracticeId = e.Id,
                Name = e.Name,
                IsActive = e.IsActive,
                Locations = e.Locations // map as needed
            };
        }
    }
}
