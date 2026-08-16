using PulseGuard.Framework.Domain;

namespace PulseGuard.PatientRegistry.Domain;

public sealed record PatientRegisteredDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    PatientId PatientId,
    string MedicalRecordNumber) : IDomainEvent;
