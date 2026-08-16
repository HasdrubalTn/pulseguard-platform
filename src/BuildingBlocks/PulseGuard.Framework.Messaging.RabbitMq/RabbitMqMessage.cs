namespace PulseGuard.Framework.Messaging.RabbitMq;

public sealed record RabbitMqMessage(
    Guid MessageId,
    DateTimeOffset OccurredOnUtc,
    int SchemaVersion,
    string EventType,
    string RoutingKey,
    ReadOnlyMemory<byte> Body);
