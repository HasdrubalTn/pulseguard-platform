namespace PulseGuard.Framework.Messaging.EntityFrameworkCore;

public sealed class InboxMessageConflictException : InvalidOperationException
{
    public InboxMessageConflictException(Guid messageId, string eventType)
        : base($"Inbox message '{messageId:D}' was already received with different content or event type '{eventType}'.")
    {
        MessageId = messageId;
        EventType = eventType;
    }

    public Guid MessageId { get; }

    public string EventType { get; }
}
