public class LookupsRepository : ILookupsRepository
{
    private readonly Container _container;

    public LookupsRepository(CosmosClient client)
    {
        _container = client.GetContainer("bfdb", "lookups");
    }

    public async Task<LookupSet?> GetLookupSetAsync(string tenantId, string lookupId)
    {
        // 1. Try tenant-specific version
        try
        {
            var tenantDoc = await _container.ReadItemAsync<LookupSet>(
                lookupId, new PartitionKey(tenantId));
            return tenantDoc.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // fallback to global
        }

        // 2. Try GLOBAL
        try
        {
            var globalDoc = await _container.ReadItemAsync<LookupSet>(
                lookupId, new PartitionKey("GLOBAL"));
            return globalDoc.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<LookupSet>> GetAllGlobalLookupsAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.tenantId = 'GLOBAL'");
        var iterator = _container.GetItemQueryIterator<LookupSet>(query);

        var results = new List<LookupSet>();
        while (iterator.HasMoreResults)
            results.AddRange(await iterator.ReadNextAsync());

        return results;
    }

    public async Task UpsertAsync(LookupSet lookup)
    {
        await _container.UpsertItemAsync(lookup, new PartitionKey(lookup.TenantId));
    }
}
