using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ec.Api.Contracts;

namespace Ec.Api.Services
{
    public class ConfigService : IConfigService
    {
        private readonly IConfigRepository _repo;

        public ConfigService(IConfigRepository repo)
        {
            _repo = repo;
        }

        public async Task<TenantConfigDto> GetTenantConfigAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            var cfg = await _repo.GetTenantConfigAsync(tenantId);
            if (cfg is null)
                throw new KeyNotFoundException("Tenant config not found.");

            return cfg;
        }

        public async Task<TenantConfigDto> UpdateTenantConfigAsync(
            string tenantId,
            TenantConfigUpdateRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            var cfg = await _repo.GetTenantConfigAsync(tenantId);
            if (cfg is null)
                throw new KeyNotFoundException("Tenant config not found.");

            // apply modifications in a safe way
            cfg.DefaultPayerIds = request.DefaultPayerIds ?? cfg.DefaultPayerIds;
            cfg.DefaultVisitTypeCode = request.DefaultVisitTypeCode ?? cfg.DefaultVisitTypeCode;
            cfg.AvailityCredentials = request.AvailityCredentials ?? cfg.AvailityCredentials;

            await _repo.SaveTenantConfigAsync(cfg);

            return cfg;
        }

        public async Task<List<PayerConfigDto>> GetPayerConfigsAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));

            return await _repo.GetPayerConfigsAsync(tenantId);
        }

        public async Task<PayerConfigDto> UpdatePayerConfigAsync(
            string tenantId,
            string payerId,
            PayerConfigUpdateRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("tenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(payerId))
                throw new ArgumentException("payerId is required.", nameof(payerId));

            var cfg = await _repo.GetPayerConfigAsync(tenantId, payerId);
            if (cfg is null)
                throw new KeyNotFoundException("Payer config not found.");

            cfg.IsEnabled = request.IsEnabled ?? cfg.IsEnabled;
            cfg.RequiresCob = request.RequiresCob ?? cfg.RequiresCob;
            cfg.PortalSettings = request.PortalSettings ?? cfg.PortalSettings;

            await _repo.SavePayerConfigAsync(cfg);

            return cfg;
        }
    }
}
