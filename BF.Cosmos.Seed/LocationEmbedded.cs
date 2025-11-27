using System.Text.Json.Serialization;

namespace BF.Cosmos.Seed.DTOs;

public class LocationEmbedded
{
    [JsonPropertyName("locationId")]
    public string LocationId { get; set; } = default!;  // "loc_001"

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("address1")]
    public string? Address1 { get; set; }

    [JsonPropertyName("address2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}