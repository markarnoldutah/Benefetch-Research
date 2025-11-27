using System.Text.Json.Serialization;

namespace BF.Cosmos.Seed.DTOs;

public class CoverageEnrollmentEmbedded
{
    [JsonPropertyName("coverageEnrollmentId")]
    public string CoverageEnrollmentId { get; set; } = default!;  // "cov_vision_1"

    [JsonPropertyName("payerId")]
    public string PayerId { get; set; } = default!;

    [JsonPropertyName("planType")]
    public string PlanType { get; set; } = default!;              // Vision, Medical, etc.

    [JsonPropertyName("memberId")]
    public string MemberId { get; set; } = default!;

    [JsonPropertyName("groupNumber")]
    public string? GroupNumber { get; set; }

    [JsonPropertyName("relationshipToSubscriber")]
    public string RelationshipToSubscriber { get; set; } = default!; // Self, Spouse, Child

    [JsonPropertyName("subscriberFirstName")]
    public string? SubscriberFirstName { get; set; }

    [JsonPropertyName("subscriberLastName")]
    public string? SubscriberLastName { get; set; }

    [JsonPropertyName("subscriberDob")]
    public DateTime? SubscriberDob { get; set; }

    [JsonPropertyName("isEmployerPlan")]
    public bool IsEmployerPlan { get; set; }

    [JsonPropertyName("isVisionPlan")]
    public bool IsVisionPlan { get; set; }

    [JsonPropertyName("isMedicalPlan")]
    public bool IsMedicalPlan { get; set; }

    [JsonPropertyName("effectiveDate")]
    public DateTime? EffectiveDate { get; set; }

    [JsonPropertyName("terminationDate")]
    public DateTime? TerminationDate { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("cobPriorityHint")]
    public byte? CobPriorityHint { get; set; }          // 1 = usually primary, 2 = secondary

    [JsonPropertyName("isCobLocked")]
    public bool IsCobLocked { get; set; }

    [JsonPropertyName("cobNotes")]
    public string? CobNotes { get; set; }
}
