namespace PulseGuard.Framework.Domain;

public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOnUtc { get; }
}
