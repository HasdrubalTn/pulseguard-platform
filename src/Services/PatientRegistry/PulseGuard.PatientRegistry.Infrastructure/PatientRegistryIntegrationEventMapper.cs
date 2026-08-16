using PulseGuard.Framework.Domain;
using PulseGuard.Framework.Messaging;
using PulseGuard.Framework.Messaging.EntityFrameworkCore;
using PulseGuard.PatientRegistry.Contracts;
using PulseGuard.PatientRegistry.Domain;

namespace PulseGuard.PatientRegistry.Infrastructure;

public sealed class PatientRegistryIntegrationEventMapper : IDomainEventMapper
{
    public IntegrationEvent? Map(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return domainEvent switch
        {
            PatientRegisteredDomainEvent patientRegistered => new PatientRegisteredIntegrationEvent(
                patientRegistered.EventId,
                patientRegistered.OccurredOnUtc,
                1,
                patientRegistered.PatientId.Value),
            _ => null,
        };
    }
}
