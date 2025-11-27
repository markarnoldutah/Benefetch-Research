using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ec.Api.Contracts;

namespace Ec.Api.Services
{
    public class PayerService : IPayerService
    {
        private readonly IPayerRepository _repo;

        public PayerService(IPayerRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<PayerDto>> SearchPayersAsync(string tenantId, string? planType, string? search)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            var results = await _repo.SearchAsync(tenantId, planType, search);
            var list = new List<PayerDto>();

            foreach (var p in results)
            {
                list.Add(new PayerDto
                {
                    PayerId = p.Id,
                    Name = p.Name,
                    PlanTypes = p.PlanTypes
                });
            }

            return list;
        }

        public async Task<PayerDto> GetPayerAsync(string tenantId, string payerId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            if (string.IsNullOrWhiteSpace(payerId))
                throw new ArgumentException("payerId is required.", nameof(payerId));

            var entity = await _repo.GetByIdAsync(tenantId, payerId);
            if (entity is null)
                throw new KeyNotFoundException("Payer not found.");

            return new PayerDto
            {
                PayerId = entity.Id,
                Name = entity.Name,
                PlanTypes = entity.PlanTypes
            };
        }
    }
}
