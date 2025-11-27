using System.Text.Json.Serialization;


namespace BF.Cosmos.Seed.DTOs;

public class TenantDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    // MIRROR of id
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "tenant";

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("billingEmail")]
    public string? BillingEmail { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("plan")]
    public string Plan { get; set; } = default!;

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}
