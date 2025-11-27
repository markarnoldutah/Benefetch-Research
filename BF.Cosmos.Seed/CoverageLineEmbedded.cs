using System.Text.Json.Serialization;

namespace BF.Cosmos.Seed.DTOs;

public class CoverageLineEmbedded
{
    [JsonPropertyName("serviceTypeCode")]
    public string ServiceTypeCode { get; set; } = default!;      // '47', '30', etc.

    [JsonPropertyName("coverageDescription")]
    public string? CoverageDescription { get; set; }

    [JsonPropertyName("copayAmount")]
    public decimal? CopayAmount { get; set; }

    [JsonPropertyName("coinsurancePercent")]
    public decimal? CoinsurancePercent { get; set; }

    [JsonPropertyName("deductibleAmount")]
    public decimal? DeductibleAmount { get; set; }

    [JsonPropertyName("remainingDeductible")]
    public decimal? RemainingDeductible { get; set; }

    [JsonPropertyName("outOfPocketMax")]
    public decimal? OutOfPocketMax { get; set; }

    [JsonPropertyName("remainingOutOfPocket")]
    public decimal? RemainingOutOfPocket { get; set; }

    [JsonPropertyName("allowanceAmount")]
    public decimal? AllowanceAmount { get; set; }

    [JsonPropertyName("networkIndicator")]
    public string? NetworkIndicator { get; set; }                // IN, OUT

    [JsonPropertyName("effectiveDate")]
    public DateTime? EffectiveDate { get; set; }

    [JsonPropertyName("terminationDate")]
    public DateTime? TerminationDate { get; set; }

    [JsonPropertyName("additionalInfo")]
    public string? AdditionalInfo { get; set; }
}
