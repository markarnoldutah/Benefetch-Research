using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Ec.Api.Contracts;
using Ec.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ec.Api.Controllers
{
    // =====================================================
    // Claims helper
    // =====================================================

    internal static class ClaimsPrincipalExtensions
    {
        public static string GetTenantIdOrThrow(this ClaimsPrincipal user)
        {
            var tenantId = user.FindFirst("tenant_id")?.Value
                           ?? user.FindFirst("tid")?.Value;

            if (string.IsNullOrWhiteSpace(tenantId))
                throw new UnauthorizedAccessException("Tenant identifier is missing.");

            return tenantId;
        }
    }

    // =====================================================
    // SessionController
    // =====================================================

    [ApiController]
    [Route("api/session")]
    [Authorize]
    public class SessionController : ControllerBase, Ec.Api.Contracts.ISessionController
    {
        private readonly ISessionService _sessionService;

        public SessionController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpGet("context")]
        public async Task<ActionResult<UserSessionContextDto>> GetContextAsync()
        {
            var context = await _sessionService.GetContextAsync(User);
            return Ok(context);
        }
    }

    // =====================================================
    // PracticesController
    // =====================================================

    [ApiController]
    [Route("api/practices")]
    [Authorize]
    public class PracticesController : ControllerBase, Ec.Api.Contracts.IPracticesController
    {
        private readonly IPracticeService _practiceService;

        public PracticesController(IPracticeService practiceService)
        {
            _practiceService = practiceService;
        }

        [HttpGet]
        public async Task<ActionResult<List<PracticeSummaryDto>>> GetPracticesAsync(
            [FromQuery] bool includeLocations = true)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var practices = await _practiceService.GetPracticesAsync(tenantId, includeLocations);
            return Ok(practices);
        }

        [HttpGet("{practiceId}")]
        public async Task<ActionResult<PracticeDetailDto>> GetPracticeAsync(string practiceId)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var practice = await _practiceService.GetPracticeAsync(tenantId, practiceId);
            return Ok(practice);
        }
    }

    // =====================================================
    // PayersController
    // =====================================================

    [ApiController]
    [Route("api/payers")]
    [Authorize]
    public class PayersController : ControllerBase, Ec.Api.Contracts.IPayersController
    {
        private readonly IPayerService _payerService;

        public PayersController(IPayerService payerService)
        {
            _payerService = payerService;
        }

        [HttpGet]
        public async Task<ActionResult<List<PayerDto>>> SearchPayersAsync(
            [FromQuery] string? planType,
            [FromQuery] string? search)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var results = await _payerService.SearchPayersAsync(tenantId, planType, search);
            return Ok(results);
        }

        [HttpGet("{payerId}")]
        public async Task<ActionResult<PayerDto>> GetPayerAsync(string payerId)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var payer = await _payerService.GetPayerAsync(tenantId, payerId);
            return Ok(payer);
        }
    }

    // =====================================================
    // PatientsController
    // =====================================================

    [ApiController]
    [Route("api/patients")]
    [Authorize]
    public class PatientsController : ControllerBase, Ec.Api.Contracts.IPatientsController
    {
        private readonly IPatientService _patientService;

        public PatientsController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        [HttpPost("search")]
        public async Task<ActionResult<PagedResult<PatientSearchResultDto>>> SearchPatientsAsync(
            [FromBody] PatientSearchRequestDto request)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var result = await _patientService.SearchPatientsAsync(tenantId, request);
            return Ok(result);
        }

        [HttpGet("{patientId}")]
        public async Task<ActionResult<PatientDetailDto>> GetPatientAsync(string patientId)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var patient = await _patientService.GetPatientAsync(tenantId, patientId);
            return Ok(patient);
        }

        [HttpPost]
        public async Task<ActionResult<PatientDetailDto>> CreatePatientAsync(
            [FromBody] PatientCreateRequestDto request)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var created = await _patientService.CreatePatientAsync(tenantId, request);

            return CreatedAtAction(
                nameof(GetPatientAsync),
                new { patientId = created.PatientId },
                created);
        }

        [HttpPut("{patientId}")]
        public async Task<ActionResult<PatientDetailDto>> UpdatePatientAsync(
            string patientId,
            [FromBody] PatientUpdateRequestDto request)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var updated = await _patientService.UpdatePatientAsync(tenantId, patientId, request);
            return Ok(updated);
        }

        [HttpPost("{patientId}/coverage-enrollments")]
        public async Task<ActionResult<CoverageEnrollmentDto>> AddCoverageEnrollmentAsync(
            string patientId,
            [FromBody] CoverageEnrollmentCreateRequestDto request)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var created = await _patientService.AddCoverageEnrollmentAsync(tenantId, patientId, request);

            return CreatedAtAction(
                nameof(GetPatientAsync),
                new { patientId },
                created);
        }

        [HttpPut("{patientId}/coverage-enrollments/{coverageEnrollmentId}")]
        public async Task<ActionResult<CoverageEnrollmentDto>> UpdateCoverageEnrollmentAsync(
            string patientId,
            string coverageEnrollmentId,
            [FromBody] CoverageEnrollmentUpdateRequestDto request)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var updated = await _patientService.UpdateCoverageEnrollmentAsync(
                tenantId,
                patientId,
                coverageEnrollmentId,
                request);

            return Ok(updated);
        }

        [HttpDelete("{patientId}/coverage-enrollments/{coverageEnrollmentId}")]
        public async Task<IActionResult> DeleteCoverageEnrollmentAsync(
            string patientId,
            string coverageEnrollmentId)
        {
            var tenantId = User.GetTenantIdOrThrow();

            await _patientService.DeleteCoverageEnrollmentAsync(
                tenantId,
                patientId,
                coverageEnrollmentId);

            return NoContent();
        }

        [HttpPost("{patientId}/encounters/search")]
        public async Task<ActionResult<PagedResult<EncounterSummaryDto>>> SearchPatientEncountersAsync(
            string patientId,
            [FromBody] PatientEncounterSearchRequestDto request)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var result = await _patientService.GetPatientEncountersAsync(
                tenantId,
                patientId,
                request);

            return Ok(result);
        }
    }

    // =====================================================
    // EncountersController
    // =====================================================

    [ApiController]
    [Route("api/encounters")]
    [Authorize]
    public class EncountersController : ControllerBase, Ec.Api.Contracts.IEncountersController
    {
        private readonly IEncounterService _encounterService;

        public EncountersController(IEncounterService encounterService)
        {
            _encounterService = encounterService;
        }

        [HttpPost]
        public async Task<ActionResult<EncounterDetailDto>> CreateEncounterAsync(
            [FromBody] EncounterCreateRequestDto request)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var created = await _encounterService.CreateEncounterAsync(tenantId, request);

            return CreatedAtAction(
                nameof(GetEncounterAsync),
                new { encounterId = created.EncounterId },
                created);
        }

        [HttpGet("{encounterId}")]
        public async Task<ActionResult<EncounterDetailDto>> GetEncounterAsync(string encounterId)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var encounter = await _encounterService.GetEncounterAsync(tenantId, encounterId);
            return Ok(encounter);
        }

        [HttpPut("{encounterId}")]
        public async Task<ActionResult<EncounterDetailDto>> UpdateEncounterAsync(
            string encounterId,
            [FromBody] EncounterUpdateRequestDto request)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var updated = await _encounterService.UpdateEncounterAsync(tenantId, encounterId, request);
            return Ok(updated);
        }

        [HttpPut("{encounterId}/coverage-decision")]
        public async Task<ActionResult<CoverageDecisionDto>> SetCoverageDecisionAsync(
            string encounterId,
            [FromBody] CoverageDecisionUpdateRequestDto request)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var decision = await _encounterService.SetCoverageDecisionAsync(tenantId, encounterId, request);
            return Ok(decision);
        }

        [HttpPost("{encounterId}/eligibility-checks")]
        public async Task<ActionResult<EligibilityCheckDto>> RunEligibilityCheckAsync(
            string encounterId,
            [FromBody] EligibilityCheckRequestDto request)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var result = await _encounterService.RunEligibilityCheckAsync(tenantId, encounterId, request);
            return Ok(result);
        }

        [HttpGet("{encounterId}/eligibility-checks")]
        public async Task<ActionResult<List<EligibilityCheckSummaryDto>>> GetEligibilityChecksAsync(
            string encounterId)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var checks = await _encounterService.GetEligibilityChecksAsync(tenantId, encounterId);
            return Ok(checks);
        }

        [HttpGet("{encounterId}/eligibility-checks/{eligibilityCheckId}")]
        public async Task<ActionResult<EligibilityCheckDto>> GetEligibilityCheckAsync(
            string encounterId,
            string eligibilityCheckId)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var check = await _encounterService.GetEligibilityCheckAsync(tenantId, encounterId, eligibilityCheckId);
            return Ok(check);
        }

        [HttpPost("search")]
        public async Task<ActionResult<PagedResult<EncounterSummaryDto>>> SearchEncountersAsync(
            [FromBody] EncounterSearchRequestDto request)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var result = await _encounterService.SearchEncountersAsync(tenantId, request);
            return Ok(result);
        }

        // From interface, but routed via PatientsController:
        [NonAction]
        public Task<ActionResult<PagedResult<EncounterSummaryDto>>> SearchPatientEncountersAsync(
            string patientId,
            PatientEncounterSearchRequestDto request)
        {
            throw new NotImplementedException("Handled by PatientsController routing.");
        }
    }

    // =====================================================
    // LookupsController
    // =====================================================

    [ApiController]
    [Route("api/lookups")]
    [Authorize]
    public class LookupsController : ControllerBase, Ec.Api.Contracts.ILookupsController
    {
        private readonly ILookupService _lookupService;

        public LookupsController(ILookupService lookupService)
        {
            _lookupService = lookupService;
        }

        [HttpGet("visit-types")]
        public async Task<ActionResult<List<LookupItemDto>>> GetVisitTypesAsync()
        {
            var items = await _lookupService.GetVisitTypesAsync();
            return Ok(items);
        }

        [HttpGet("cob-reasons")]
        public async Task<ActionResult<List<LookupItemDto>>> GetCobReasonsAsync()
        {
            var items = await _lookupService.GetCobReasonsAsync();
            return Ok(items);
        }

        [HttpGet("visit-type-service-types")]
        public async Task<ActionResult<List<VisitTypeServiceTypesDto>>> GetVisitTypeServiceTypesAsync()
        {
            var items = await _lookupService.GetVisitTypeServiceTypesAsync();
            return Ok(items);
        }
    }

    // =====================================================
    // ConfigController
    // =====================================================

    [ApiController]
    [Route("api/config")]
    [Authorize(Roles = "Admin,EC-Admin")]
    public class ConfigController : ControllerBase, Ec.Api.Contracts.IConfigController
    {
        private readonly IConfigService _configService;

        public ConfigController(IConfigService configService)
        {
            _configService = configService;
        }

        [HttpGet("tenant")]
        public async Task<ActionResult<TenantConfigDto>> GetTenantConfigAsync()
        {
            var tenantId = User.GetTenantIdOrThrow();
            var cfg = await _configService.GetTenantConfigAsync(tenantId);
            return Ok(cfg);
        }

        [HttpPut("tenant")]
        public async Task<ActionResult<TenantConfigDto>> UpdateTenantConfigAsync(
            [FromBody] TenantConfigUpdateRequestDto request)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var updated = await _configService.UpdateTenantConfigAsync(tenantId, request);
            return Ok(updated);
        }

        [HttpGet("payers")]
        public async Task<ActionResult<List<PayerConfigDto>>> GetPayerConfigsAsync()
        {
            var tenantId = User.GetTenantIdOrThrow();
            var list = await _configService.GetPayerConfigsAsync(tenantId);
            return Ok(list);
        }

        [HttpPut("payers/{payerId}")]
        public async Task<ActionResult<PayerConfigDto>> UpdatePayerConfigAsync(
            string payerId,
            [FromBody] PayerConfigUpdateRequestDto request)
        {
            var tenantId = User.GetTenantIdOrThrow();
            var updated = await _configService.UpdatePayerConfigAsync(tenantId, payerId, request);
            return Ok(updated);
        }
    }
}
