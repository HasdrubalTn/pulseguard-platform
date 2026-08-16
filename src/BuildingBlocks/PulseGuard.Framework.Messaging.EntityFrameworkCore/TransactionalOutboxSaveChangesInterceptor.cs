using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PulseGuard.Framework.Domain;
using PulseGuard.Framework.Messaging;

namespace PulseGuard.Framework.Messaging.EntityFrameworkCore;

public sealed class TransactionalOutboxSaveChangesInterceptor(
    IEnumerable<IDomainEventMapper> mappers,
    IIntegrationEventSerializer serializer) : SaveChangesInterceptor
{
    private readonly IReadOnlyCollection<IDomainEventMapper> _mappers = mappers.ToArray();
    private readonly IIntegrationEventSerializer _serializer = serializer;
    private IReadOnlyCollection<IHasDomainEvents> _pendingAggregates = [];
    private IReadOnlyCollection<OutboxMessage> _pendingOutboxMessages = [];

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CaptureDomainEvents(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureDomainEvents(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ClearCommittedDomainEvents();
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ClearCommittedDomainEvents();
        return ValueTask.FromResult(result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        DetachUncommittedOutboxMessages(eventData.Context);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        DetachUncommittedOutboxMessages(eventData.Context);
        return Task.CompletedTask;
    }

    public override void SaveChangesCanceled(DbContextEventData eventData)
    {
        DetachUncommittedOutboxMessages(eventData.Context);
    }

    public override Task SaveChangesCanceledAsync(
        DbContextEventData eventData,
        CancellationToken cancellationToken = default)
    {
        DetachUncommittedOutboxMessages(eventData.Context);
        return Task.CompletedTask;
    }

    private void CaptureDomainEvents(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        IHasDomainEvents[] aggregates = dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToArray();

        List<OutboxMessage> outboxMessages = [];
        _pendingAggregates = aggregates;
        foreach (IHasDomainEvents aggregate in aggregates)
        {
            foreach (IDomainEvent domainEvent in aggregate.DomainEvents)
            {
                IntegrationEvent? integrationEvent = Map(domainEvent);
                if (integrationEvent is null ||
                    dbContext.Set<OutboxMessage>().Local.Any(message => message.Id == integrationEvent.EventId))
                {
                    continue;
                }

                OutboxMessage outboxMessage = OutboxMessage.Create(integrationEvent, _serializer);
                dbContext.Set<OutboxMessage>().Add(outboxMessage);
                outboxMessages.Add(outboxMessage);
            }
        }

        _pendingOutboxMessages = outboxMessages;
    }

    private IntegrationEvent? Map(IDomainEvent domainEvent)
    {
        foreach (IDomainEventMapper mapper in _mappers)
        {
            IntegrationEvent? integrationEvent = mapper.Map(domainEvent);
            if (integrationEvent is not null)
            {
                return integrationEvent;
            }
        }

        return null;
    }

    private void ClearCommittedDomainEvents()
    {
        // Events remain on the aggregate when the database transaction fails, so a caller can safely retry.
        foreach (IHasDomainEvents aggregate in _pendingAggregates)
        {
            aggregate.ClearDomainEvents();
        }

        _pendingAggregates = [];
        _pendingOutboxMessages = [];
    }

    private void DetachUncommittedOutboxMessages(DbContext? dbContext)
    {
        if (dbContext is not null)
        {
            foreach (OutboxMessage outboxMessage in _pendingOutboxMessages)
            {
                dbContext.Entry(outboxMessage).State = EntityState.Detached;
            }
        }

        // Domain Events are intentionally retained so the complete transaction can be retried.
        _pendingOutboxMessages = [];
    }
}
