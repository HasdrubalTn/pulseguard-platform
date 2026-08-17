using PulseGuard.Framework.Messaging;

namespace PulseGuard.PatientRegistry.Contracts;

public sealed record PatientRegisteredIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    int SchemaVersion,
    Guid PatientId) : IntegrationEvent(EventId, OccurredOnUtc, SchemaVersion);
