public class PracticeDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    // MIRROR of id
    [JsonPropertyName("practiceId")]
    public string PracticeId { get; set; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "practice";

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("externalRef")]
    public string? ExternalRef { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("locations")]
    public List<LocationEmbedded> Locations { get; set; } = new();

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}
