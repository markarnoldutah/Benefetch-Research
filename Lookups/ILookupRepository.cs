public interface ILookupsRepository
{
    Task<LookupSet?> GetLookupSetAsync(string tenantId, string lookupId);
    Task<IReadOnlyList<LookupSet>> GetAllGlobalLookupsAsync();
    Task UpsertAsync(LookupSet lookup);
}
