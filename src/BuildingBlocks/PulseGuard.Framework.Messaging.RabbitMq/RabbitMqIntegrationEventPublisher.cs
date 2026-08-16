using System.Text.Json;
using Microsoft.Extensions.Options;
using PulseGuard.Framework.Messaging;

namespace PulseGuard.Framework.Messaging.RabbitMq;

public sealed class RabbitMqIntegrationEventPublisher(
    IRabbitMqMessagePublisher publisher,
    IOptions<RabbitMqOptions> options) : IIntegrationEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _options = options.Value;

    public ValueTask PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        Type eventType = integrationEvent.GetType();
        if (!_options.Routes.TryGetValue(eventType.Name, out string? routingKey) ||
            string.IsNullOrWhiteSpace(routingKey))
        {
            throw new InvalidOperationException($"No RabbitMQ route is configured for integration event '{eventType.Name}'.");
        }

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, eventType, SerializerOptions);
        RabbitMqMessage message = new(
            integrationEvent.EventId,
            integrationEvent.OccurredOnUtc,
            integrationEvent.SchemaVersion,
            eventType.Name,
            routingKey,
            body);

        return publisher.PublishAsync(message, cancellationToken);
    }
}
