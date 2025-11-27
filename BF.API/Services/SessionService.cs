using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Ec.Api.Contracts;

namespace Ec.Api.Services
{
    public class SessionService : ISessionService
    {
        private readonly ITenantAccessService _tenantAccessService;

        public SessionService(ITenantAccessService tenantAccessService)
        {
            _tenantAccessService = tenantAccessService;
        }

        public async Task<UserSessionContextDto> GetContextAsync(ClaimsPrincipal user)
        {
            if (user is null)
                throw new ArgumentNullException(nameof(user));

            var tenantId = user.FindFirst("tenant_id")?.Value
                           ?? user.FindFirst("tid")?.Value;

            if (string.IsNullOrWhiteSpace(tenantId))
                throw new UnauthorizedAccessException("Tenant identifier is missing.");

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("User identifier is missing.");

            var hasTenantAccess = await _tenantAccessService.HasAccessAsync(userId, tenantId);
            if (!hasTenantAccess)
                throw new UnauthorizedAccessException("User does not have access to this tenant.");

            // Map to your actual session context
            return new UserSessionContextDto
            {
                TenantId = tenantId,
                UserId = userId,
                DisplayName = user.Identity?.Name ?? userId,
                Roles = _tenantAccessService.GetRolesForUser(user)
            };
        }
    }
}
