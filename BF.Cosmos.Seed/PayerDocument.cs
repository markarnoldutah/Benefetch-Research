using System.Text.Json.Serialization;


namespace BF.Cosmos.Seed.DTOs;

public class PayerDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    // MIRROR of id
    [JsonPropertyName("payerId")]
    public string PayerId { get; set; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "payer";

    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("planType")]
    public string PlanType { get; set; } = default!;

    [JsonPropertyName("availityPayerCode")]
    public string? AvailityPayerCode { get; set; }

    [JsonPropertyName("x12PayerId")]
    public string? X12PayerId { get; set; }

    [JsonPropertyName("isMedicare")]
    public bool IsMedicare { get; set; }

    [JsonPropertyName("isMedicaid")]
    public bool IsMedicaid { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}
