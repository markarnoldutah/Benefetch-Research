using System.Text.Json.Serialization;

namespace BF.Cosmos.Seed.DTOs;

public class EligibilityCheckEmbedded
{
    [JsonPropertyName("eligibilityCheckId")]
    public string EligibilityCheckId { get; set; } = default!;

    [JsonPropertyName("coverageEnrollmentId")]
    public string CoverageEnrollmentId { get; set; } = default!;

    [JsonPropertyName("payerId")]
    public string PayerId { get; set; } = default!;

    [JsonPropertyName("dateOfService")]
    public DateTime DateOfService { get; set; }

    [JsonPropertyName("requestedAtUtc")]
    public DateTime RequestedAtUtc { get; set; }

    [JsonPropertyName("completedAtUtc")]
    public DateTime? CompletedAtUtc { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";     // Pending, Succeeded, Failed

    [JsonPropertyName("rawStatusCode")]
    public string? RawStatusCode { get; set; }

    [JsonPropertyName("rawStatusDescription")]
    public string? RawStatusDescription { get; set; }

    // Snapshots at time of check
    [JsonPropertyName("memberIdSnapshot")]
    public string MemberIdSnapshot { get; set; } = default!;

    [JsonPropertyName("groupNumberSnapshot")]
    public string? GroupNumberSnapshot { get; set; }

    [JsonPropertyName("planNameSnapshot")]
    public string? PlanNameSnapshot { get; set; }

    [JsonPropertyName("effectiveDateSnapshot")]
    public DateTime? EffectiveDateSnapshot { get; set; }

    [JsonPropertyName("terminationDateSnapshot")]
    public DateTime? TerminationDateSnapshot { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("coverageLines")]
    public List<CoverageLineEmbedded> CoverageLines { get; set; } = new();

    [JsonPropertyName("payloads")]
    public List<EligibilityPayloadEmbedded> Payloads { get; set; } = new();
}
