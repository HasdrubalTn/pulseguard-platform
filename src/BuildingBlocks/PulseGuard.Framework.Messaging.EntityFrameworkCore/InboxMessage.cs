using System.Security.Cryptography;

namespace PulseGuard.Framework.Messaging.EntityFrameworkCore;

public sealed class InboxMessage
{
    private InboxMessage()
    {
    }

    private InboxMessage(
        Guid id,
        string eventType,
        string contentHash,
        DateTimeOffset receivedOnUtc)
    {
        Id = id;
        EventType = eventType;
        ContentHash = contentHash;
        ReceivedOnUtc = receivedOnUtc;
    }

    public Guid Id { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string ContentHash { get; private set; } = string.Empty;

    public DateTimeOffset ReceivedOnUtc { get; private set; }

    public DateTimeOffset? ProcessedOnUtc { get; private set; }

    public static InboxMessage Receive(
        Guid id,
        string eventType,
        ReadOnlySpan<byte> content,
        DateTimeOffset receivedOnUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        if (content.IsEmpty)
        {
            throw new ArgumentException("Inbox message content cannot be empty.", nameof(content));
        }

        string contentHash = Convert.ToHexString(SHA256.HashData(content));
        return new InboxMessage(id, eventType, contentHash, receivedOnUtc);
    }

    public void MarkProcessed(DateTimeOffset processedOnUtc)
    {
        ProcessedOnUtc = processedOnUtc;
    }
}
