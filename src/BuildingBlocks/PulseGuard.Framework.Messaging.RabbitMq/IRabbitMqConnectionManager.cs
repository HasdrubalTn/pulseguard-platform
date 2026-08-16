using RabbitMQ.Client;

namespace PulseGuard.Framework.Messaging.RabbitMq;

internal interface IRabbitMqConnectionManager
{
    ValueTask<IConnection> GetConnectionAsync(CancellationToken cancellationToken);
}
