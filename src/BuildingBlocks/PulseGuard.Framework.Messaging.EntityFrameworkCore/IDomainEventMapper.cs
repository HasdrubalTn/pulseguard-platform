using PulseGuard.Framework.Domain;
using PulseGuard.Framework.Messaging;

namespace PulseGuard.Framework.Messaging.EntityFrameworkCore;

public interface IDomainEventMapper
{
    IntegrationEvent? Map(IDomainEvent domainEvent);
}
