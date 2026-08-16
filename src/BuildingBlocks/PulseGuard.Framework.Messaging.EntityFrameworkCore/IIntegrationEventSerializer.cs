using PulseGuard.Framework.Messaging;

namespace PulseGuard.Framework.Messaging.EntityFrameworkCore;

public interface IIntegrationEventSerializer
{
    string Serialize(IntegrationEvent integrationEvent);

    IntegrationEvent Deserialize(string eventType, string payload);
}
