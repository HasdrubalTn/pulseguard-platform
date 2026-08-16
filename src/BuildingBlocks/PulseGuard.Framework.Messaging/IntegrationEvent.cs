namespace PulseGuard.Framework.Messaging;

public abstract record IntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    int SchemaVersion);
