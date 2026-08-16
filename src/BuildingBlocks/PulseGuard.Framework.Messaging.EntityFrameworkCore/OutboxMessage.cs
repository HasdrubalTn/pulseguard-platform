using PulseGuard.Framework.Messaging;

namespace PulseGuard.Framework.Messaging.EntityFrameworkCore;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    private OutboxMessage(
        Guid id,
        DateTimeOffset occurredOnUtc,
        int schemaVersion,
        string eventType,
        string payload)
    {
        Id = id;
        OccurredOnUtc = occurredOnUtc;
        SchemaVersion = schemaVersion;
        EventType = eventType;
        Payload = payload;
    }

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredOnUtc { get; private set; }

    public int SchemaVersion { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset? PublishedOnUtc { get; private set; }

    public static OutboxMessage Create(
        IntegrationEvent integrationEvent,
        IIntegrationEventSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentNullException.ThrowIfNull(serializer);

        return new OutboxMessage(
            integrationEvent.EventId,
            integrationEvent.OccurredOnUtc,
            integrationEvent.SchemaVersion,
            integrationEvent.GetType().Name,
            serializer.Serialize(integrationEvent));
    }

    public void MarkPublished(DateTimeOffset publishedOnUtc)
    {
        PublishedOnUtc = publishedOnUtc;
    }
}
