using PulseGuard.Framework.Messaging;

namespace PulseGuard.DeviceGateway;

public sealed record Hl7MessageReceivedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    int SchemaVersion,
    string MessageCode,
    string TriggerEvent,
    string ControlId,
    string SendingApplication,
    string SendingFacility) : IntegrationEvent(EventId, OccurredOnUtc, SchemaVersion);
