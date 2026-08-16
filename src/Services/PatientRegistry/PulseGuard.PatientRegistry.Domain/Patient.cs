using PulseGuard.Framework.Domain;

namespace PulseGuard.PatientRegistry.Domain;

public sealed class Patient : AggregateRoot<PatientId>
{
    private Patient(
        PatientId id,
        string medicalRecordNumber,
        string givenName,
        string familyName,
        DateOnly birthDate,
        DateTimeOffset registeredOnUtc)
        : base(id)
    {
        MedicalRecordNumber = medicalRecordNumber;
        GivenName = givenName;
        FamilyName = familyName;
        BirthDate = birthDate;
        RegisteredOnUtc = registeredOnUtc;
    }

    public string MedicalRecordNumber { get; }

    public string GivenName { get; }

    public string FamilyName { get; }

    public DateOnly BirthDate { get; }

    public DateTimeOffset RegisteredOnUtc { get; }

    public static Patient Register(
        string medicalRecordNumber,
        string givenName,
        string familyName,
        DateOnly birthDate,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(medicalRecordNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(givenName);
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentNullException.ThrowIfNull(timeProvider);

        DateTimeOffset now = timeProvider.GetUtcNow();
        if (birthDate > DateOnly.FromDateTime(now.UtcDateTime))
        {
            throw new ArgumentOutOfRangeException(nameof(birthDate), "Birth date cannot be in the future.");
        }

        Patient patient = new(
            PatientId.New(),
            NormalizeMedicalRecordNumber(medicalRecordNumber),
            givenName.Trim(),
            familyName.Trim(),
            birthDate,
            now);

        patient.Raise(new PatientRegisteredDomainEvent(
            Guid.NewGuid(),
            now,
            patient.Id,
            patient.MedicalRecordNumber));

        return patient;
    }

    private static string NormalizeMedicalRecordNumber(string value) => value.Trim().ToUpperInvariant();
}
