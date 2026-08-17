using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PulseGuard.Framework.Messaging;

namespace PulseGuard.Framework.Messaging.EntityFrameworkCore;

internal sealed partial class OutboxDispatcher<TDbContext>(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<TransactionalMessagingOptions> options,
    ILogger<OutboxDispatcher<TDbContext>> logger) : BackgroundService
    where TDbContext : DbContext
{
    private const string InstrumentationName = "PulseGuard.Framework.Messaging.EntityFrameworkCore";
    private static readonly ActivitySource ActivitySource = new(InstrumentationName);
    private static readonly Meter Meter = new(InstrumentationName);
    private static readonly Counter<long> PublishedMessages = Meter.CreateCounter<long>(
        "pulseguard.messaging.outbox.published",
        description: "Number of Outbox messages confirmed by the transport and marked as published.");
    private static readonly Counter<long> FailedMessages = Meter.CreateCounter<long>(
        "pulseguard.messaging.outbox.failed",
        description: "Number of Outbox dispatch attempts that failed.");
    private readonly TransactionalMessagingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            bool batchWasFull = false;

            try
            {
                batchWasFull = await DispatchBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogDispatchCycleFailed(logger, exception);
            }

            if (!batchWasFull)
            {
                await Task.Delay(_options.PollingInterval, timeProvider, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<bool> DispatchBatchAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        TDbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        IIntegrationEventPublisher publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
        IIntegrationEventSerializer serializer = scope.ServiceProvider.GetRequiredService<IIntegrationEventSerializer>();

        List<OutboxMessage> messages = await dbContext.Set<OutboxMessage>()
            .Where(message => message.PublishedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .ThenBy(message => message.Id)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (OutboxMessage message in messages)
        {
            using Activity? activity = ActivitySource.StartActivity("outbox.publish", ActivityKind.Producer);
            activity?.SetTag("messaging.message.id", message.Id);
            activity?.SetTag("messaging.message.type", message.EventType);

            try
            {
                IntegrationEvent integrationEvent = serializer.Deserialize(message.EventType, message.Payload);
                await publisher.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

                // A crash after the broker confirm and before this commit can redeliver the event.
                // Consumers therefore use the Inbox message ID as their idempotency key.
                message.MarkPublished(timeProvider.GetUtcNow());
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                PublishedMessages.Add(1, new KeyValuePair<string, object?>("event.type", message.EventType));
                LogMessagePublished(logger, message.Id, message.EventType);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
                FailedMessages.Add(1, new KeyValuePair<string, object?>("event.type", message.EventType));
                LogMessagePublishFailed(logger, exception, message.Id, message.EventType);
                throw;
            }
        }

        return messages.Count == _options.BatchSize;
    }

    [LoggerMessage(
        EventId = 2100,
        Level = LogLevel.Debug,
        Message = "Published Outbox message {MessageId} of type {EventType}")]
    private static partial void LogMessagePublished(ILogger logger, Guid messageId, string eventType);

    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Error,
        Message = "Unable to publish Outbox message {MessageId} of type {EventType}")]
    private static partial void LogMessagePublishFailed(
        ILogger logger,
        Exception exception,
        Guid messageId,
        string eventType);

    [LoggerMessage(
        EventId = 2102,
        Level = LogLevel.Error,
        Message = "The Outbox dispatch cycle failed")]
    private static partial void LogDispatchCycleFailed(ILogger logger, Exception exception);
}
