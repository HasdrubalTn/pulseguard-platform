using PulseGuard.Framework.Messaging;

namespace PulseGuard.DeviceGateway;

public sealed partial class LoggingIntegrationEventPublisher(ILogger<LoggingIntegrationEventPublisher> logger)
    : IIntegrationEventPublisher
{
    public ValueTask PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogIntegrationEventPublished(
            logger,
            integrationEvent.GetType().Name,
            integrationEvent.EventId,
            integrationEvent.SchemaVersion);

        return ValueTask.CompletedTask;
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Published integration event {EventType} with ID {EventId} and schema version {SchemaVersion}")]
    private static partial void LogIntegrationEventPublished(
        ILogger logger,
        string eventType,
        Guid eventId,
        int schemaVersion);
}
