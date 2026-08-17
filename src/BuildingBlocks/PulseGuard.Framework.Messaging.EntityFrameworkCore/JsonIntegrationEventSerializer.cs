using System.Text.Json;
using Microsoft.Extensions.Options;
using PulseGuard.Framework.Messaging;

namespace PulseGuard.Framework.Messaging.EntityFrameworkCore;

public sealed class JsonIntegrationEventSerializer(IOptions<TransactionalMessagingOptions> options)
    : IIntegrationEventSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly TransactionalMessagingOptions _options = options.Value;

    public string Serialize(IntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions);
    }

    public IntegrationEvent Deserialize(string eventType, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        if (!_options.EventTypes.TryGetValue(eventType, out Type? integrationEventType))
        {
            throw new InvalidOperationException($"Integration event type '{eventType}' is not registered.");
        }

        return JsonSerializer.Deserialize(payload, integrationEventType, SerializerOptions) as IntegrationEvent
            ?? throw new InvalidOperationException($"Unable to deserialize integration event '{eventType}'.");
    }
}
