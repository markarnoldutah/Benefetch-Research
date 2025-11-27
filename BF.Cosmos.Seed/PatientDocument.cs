using System.Text.Json.Serialization;


namespace BF.Cosmos.Seed.DTOs;

public class PatientDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    // MIRROR of id
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "patient";

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = default!;

    [JsonPropertyName("practiceId")]
    public string PracticeId { get; set; } = default!;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = default!;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = default!;

    [JsonPropertyName("dateOfBirth")]
    public DateTime? DateOfBirth { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("coverageEnrollments")]
    public List<CoverageEnrollmentEmbedded> CoverageEnrollments { get; set; } = new();

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}
