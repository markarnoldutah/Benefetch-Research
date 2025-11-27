using System.Text.Json.Serialization;

namespace BF.Cosmos.Seed.DTOs;

public class CoverageDecisionEmbedded
{
    [JsonPropertyName("encounterCoverageDecisionId")]
    public string EncounterCoverageDecisionId { get; set; } = default!;

    [JsonPropertyName("primaryCoverageEnrollmentId")]
    public string PrimaryCoverageEnrollmentId { get; set; } = default!;

    [JsonPropertyName("secondaryCoverageEnrollmentId")]
    public string? SecondaryCoverageEnrollmentId { get; set; }

    [JsonPropertyName("cobReason")]
    public string CobReason { get; set; } = default!;   // e.g. RoutineVision_UseVisionPlanPrimary

    [JsonPropertyName("overriddenByUser")]
    public bool OverriddenByUser { get; set; }

    [JsonPropertyName("overrideNote")]
    public string? OverrideNote { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("createdByUserId")]
    public string? CreatedByUserId { get; set; }
}
