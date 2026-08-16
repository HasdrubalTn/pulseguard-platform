namespace PulseGuard.Framework.Messaging;

public interface IIntegrationEventPublisher
{
    ValueTask PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
