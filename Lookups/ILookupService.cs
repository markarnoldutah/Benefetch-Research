public interface ILookupsService
{
    Task<LookupSet?> GetLookupAsync(string tenantId, string lookupId);
    Task<IReadOnlyList<LookupItem>> GetItemsAsync(string tenantId, string lookupId);
}
