public class EncounterDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    // MIRROR of id
    [JsonPropertyName("encounterId")]
    public string EncounterId { get; set; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "encounter";

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = default!;

    [JsonPropertyName("practiceId")]
    public string PracticeId { get; set; } = default!;

    [JsonPropertyName("locationId")]
    public string? LocationId { get; set; }

    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = default!;  // foreign key

    [JsonPropertyName("visitDate")]
    public DateTime VisitDate { get; set; }

    [JsonPropertyName("visitType")]
    public string VisitType { get; set; } = default!;

    [JsonPropertyName("externalRef")]
    public string? ExternalRef { get; set; }

    [JsonPropertyName("coverageDecision")]
    public CoverageDecisionEmbedded? CoverageDecision { get; set; }

    [JsonPropertyName("eligibilityChecks")]
    public List<EligibilityCheckEmbedded> EligibilityChecks { get; set; } = new();

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("createdByUserId")]
    public string? CreatedByUserId { get; set; }
}
