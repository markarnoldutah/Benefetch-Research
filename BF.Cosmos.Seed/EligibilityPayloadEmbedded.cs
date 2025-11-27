using System.Text.Json.Serialization;

namespace BF.Cosmos.Seed.DTOs;

public class EligibilityPayloadEmbedded
{
    [JsonPropertyName("payloadId")]
    public string PayloadId { get; set; } = default!;

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = default!;            // Request, Response

    [JsonPropertyName("format")]
    public string Format { get; set; } = default!;               // X12_270, X12_271, JSON

    [JsonPropertyName("storageUrl")]
    public string StorageUrl { get; set; } = default!;

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }
}



