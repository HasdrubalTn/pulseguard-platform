using System.Text.Json.Serialization;

namespace PulseGuard.PatientRegistry.Contracts;

public sealed record RegisterPatientRequest(
    string MedicalRecordNumber,
    string GivenName,
    string FamilyName,
    DateOnly BirthDate);

public sealed record PatientResponse(
    Guid Id,
    string MedicalRecordNumber,
    string GivenName,
    string FamilyName,
    DateOnly BirthDate,
    DateTimeOffset RegisteredOnUtc);

public sealed record FhirPatientResource(
    [property: JsonPropertyName("resourceType")] string ResourceType,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("identifier")] IReadOnlyList<FhirIdentifier> Identifier,
    [property: JsonPropertyName("name")] IReadOnlyList<FhirHumanName> Name,
    [property: JsonPropertyName("birthDate")] string BirthDate,
    [property: JsonPropertyName("active")] bool Active);

public sealed record FhirIdentifier(
    [property: JsonPropertyName("system")] string System,
    [property: JsonPropertyName("value")] string Value);

public sealed record FhirHumanName(
    [property: JsonPropertyName("use")] string Use,
    [property: JsonPropertyName("family")] string Family,
    [property: JsonPropertyName("given")] IReadOnlyList<string> Given);
