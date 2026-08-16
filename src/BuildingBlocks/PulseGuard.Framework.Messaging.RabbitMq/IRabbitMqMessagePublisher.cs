namespace PulseGuard.Framework.Messaging.RabbitMq;

public interface IRabbitMqMessagePublisher
{
    ValueTask PublishAsync(RabbitMqMessage message, CancellationToken cancellationToken);
}
