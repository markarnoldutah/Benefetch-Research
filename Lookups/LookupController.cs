[ApiController]
[Route("api/tenants/{tenantId}/lookups")]
public class LookupsController : ControllerBase
{
    private readonly ILookupService _service;

    public LookupsController(ILookupService service)
    {
        _service = service;
    }

    [HttpGet("{lookupId}")]
    public async Task<IActionResult> GetLookupSet(
        string tenantId, string lookupId)
    {
        var set = await _service.GetLookupAsync(tenantId, lookupId);
        return set is null ? NotFound() : Ok(set);
    }

    [HttpGet("{lookupId}/items")]
    public async Task<IActionResult> GetLookupItems(
        string tenantId, string lookupId)
    {
        var items = await _service.GetItemsAsync(tenantId, lookupId);
        return Ok(items);
    }
}
