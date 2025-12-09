public class LookupsService : ILookupService
{
    private readonly ILookupsRepository _repo;

    public LookupsService(ILookupsRepository repo)
    {
        _repo = repo;
    }

    public async Task<LookupSet?> GetLookupAsync(string tenantId, string lookupId)
    {
        return await _repo.GetLookupSetAsync(tenantId, lookupId);
    }

    public async Task<IReadOnlyList<LookupItem>> GetItemsAsync(string tenantId, string lookupId)
    {
        var set = await _repo.GetLookupSetAsync(tenantId, lookupId);
        return set?.Items?.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToList()
               ?? new List<LookupItem>();
    }
}
