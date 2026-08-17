using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace PulseGuard.Framework.Messaging.EntityFrameworkCore;

public sealed partial class TransactionalInboxProcessor<TDbContext>(
    IDbContextFactory<TDbContext> dbContextFactory,
    TimeProvider timeProvider,
    ILogger<TransactionalInboxProcessor<TDbContext>> logger)
    where TDbContext : DbContext
{
    private const string InstrumentationName = "PulseGuard.Framework.Messaging.EntityFrameworkCore";
    private static readonly ActivitySource ActivitySource = new(InstrumentationName);
    private static readonly Meter Meter = new(InstrumentationName);
    private static readonly Counter<long> ProcessedMessages = Meter.CreateCounter<long>(
        "pulseguard.messaging.inbox.processed",
        description: "Number of Inbox messages committed with their consumer side effects.");
    private static readonly Counter<long> DuplicateMessages = Meter.CreateCounter<long>(
        "pulseguard.messaging.inbox.duplicates",
        description: "Number of duplicate Inbox messages suppressed.");
    private static readonly Counter<long> ConflictingMessages = Meter.CreateCounter<long>(
        "pulseguard.messaging.inbox.conflicts",
        description: "Number of Inbox IDs reused with different content.");

    public async Task<InboxProcessingResult> ProcessAsync(
        Guid messageId,
        string eventType,
        ReadOnlyMemory<byte> content,
        Func<TDbContext, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Inbox message ID cannot be empty.", nameof(messageId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        if (content.IsEmpty)
        {
            throw new ArgumentException("Inbox message content cannot be empty.", nameof(content));
        }

        ArgumentNullException.ThrowIfNull(handler);

        using Activity? activity = ActivitySource.StartActivity("inbox.process", ActivityKind.Consumer);
        activity?.SetTag("messaging.message.id", messageId);
        activity?.SetTag("messaging.message.type", eventType);

        await using TDbContext dbContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        InboxMessage? inboxMessage = await dbContext.Set<InboxMessage>()
            .SingleOrDefaultAsync(message => message.Id == messageId, cancellationToken)
            .ConfigureAwait(false);

        if (inboxMessage is not null)
        {
            EnsureMatchingContent(inboxMessage, eventType, content.Span);
            if (inboxMessage.ProcessedOnUtc is not null)
            {
                RecordDuplicate(messageId, eventType);
                return InboxProcessingResult.Duplicate;
            }
        }
        else
        {
            inboxMessage = InboxMessage.Receive(messageId, eventType, content.Span, timeProvider.GetUtcNow());
            dbContext.Set<InboxMessage>().Add(inboxMessage);
        }

        await handler(dbContext, cancellationToken).ConfigureAwait(false);
        inboxMessage.MarkProcessed(timeProvider.GetUtcNow());

        try
        {
            // EF Core wraps this SaveChanges call in one local transaction, so the Inbox row and
            // every side effect tracked by the handler either commit together or roll back together.
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception)
        {
            InboxProcessingResult? concurrentResult = await TryClassifyConcurrentDuplicateAsync(
                messageId,
                eventType,
                content,
                cancellationToken).ConfigureAwait(false);
            if (concurrentResult is not null)
            {
                return concurrentResult.Value;
            }

            LogProcessingFailed(logger, messageId, eventType, exception);
            throw;
        }

        ProcessedMessages.Add(1, new KeyValuePair<string, object?>("event.type", eventType));
        LogMessageProcessed(logger, messageId, eventType);
        return InboxProcessingResult.Processed;
    }

    private async Task<InboxProcessingResult?> TryClassifyConcurrentDuplicateAsync(
        Guid messageId,
        string eventType,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        await using TDbContext verificationContext = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        InboxMessage? committedMessage = await verificationContext.Set<InboxMessage>()
            .AsNoTracking()
            .SingleOrDefaultAsync(message => message.Id == messageId, cancellationToken)
            .ConfigureAwait(false);

        if (committedMessage?.ProcessedOnUtc is null)
        {
            return null;
        }

        EnsureMatchingContent(committedMessage, eventType, content.Span);
        RecordDuplicate(messageId, eventType);
        return InboxProcessingResult.Duplicate;
    }

    private void EnsureMatchingContent(InboxMessage message, string eventType, ReadOnlySpan<byte> content)
    {
        if (message.HasSameContent(eventType, content))
        {
            return;
        }

        ConflictingMessages.Add(1, new KeyValuePair<string, object?>("event.type", eventType));
        LogMessageConflict(logger, message.Id, eventType);
        throw new InboxMessageConflictException(message.Id, eventType);
    }

    private void RecordDuplicate(Guid messageId, string eventType)
    {
        DuplicateMessages.Add(1, new KeyValuePair<string, object?>("event.type", eventType));
        LogDuplicateSuppressed(logger, messageId, eventType);
    }

    [LoggerMessage(
        EventId = 1110,
        Level = LogLevel.Debug,
        Message = "Committed Inbox message {MessageId} of type {EventType}")]
    private static partial void LogMessageProcessed(ILogger logger, Guid messageId, string eventType);

    [LoggerMessage(
        EventId = 1111,
        Level = LogLevel.Information,
        Message = "Suppressed duplicate Inbox message {MessageId} of type {EventType}")]
    private static partial void LogDuplicateSuppressed(ILogger logger, Guid messageId, string eventType);

    [LoggerMessage(
        EventId = 1112,
        Level = LogLevel.Warning,
        Message = "Rejected conflicting Inbox message {MessageId} of type {EventType}")]
    private static partial void LogMessageConflict(ILogger logger, Guid messageId, string eventType);

    [LoggerMessage(
        EventId = 1113,
        Level = LogLevel.Error,
        Message = "Failed to commit Inbox message {MessageId} of type {EventType}")]
    private static partial void LogProcessingFailed(
        ILogger logger,
        Guid messageId,
        string eventType,
        Exception exception);
}
