using PulseGuard.Framework.Messaging;

namespace PulseGuard.Framework.Messaging.EntityFrameworkCore;

public sealed class TransactionalMessagingOptions
{
    private readonly Dictionary<string, Type> _eventTypes = new(StringComparer.Ordinal);

    public int BatchSize { get; set; } = 50;

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    internal IReadOnlyDictionary<string, Type> EventTypes => _eventTypes;

    public void RegisterEvent<TIntegrationEvent>()
        where TIntegrationEvent : IntegrationEvent
    {
        Type eventType = typeof(TIntegrationEvent);
        string eventName = eventType.Name;

        if (_eventTypes.TryGetValue(eventName, out Type? existingType) && existingType != eventType)
        {
            throw new InvalidOperationException(
                $"Integration event name '{eventName}' is already registered for '{existingType.FullName}'.");
        }

        _eventTypes[eventName] = eventType;
    }
}
