using PulseGuard.Framework.Messaging;

namespace PulseGuard.DeviceGateway;

public sealed class LoggingIntegrationEventPublisher(ILogger<LoggingIntegrationEventPublisher> logger)
    : IIntegrationEventPublisher
{
    public ValueTask PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation(
            "Published integration event {EventType} with ID {EventId} and schema version {SchemaVersion}",
            integrationEvent.GetType().Name,
            integrationEvent.EventId,
            integrationEvent.SchemaVersion);

        return ValueTask.CompletedTask;
    }
}
