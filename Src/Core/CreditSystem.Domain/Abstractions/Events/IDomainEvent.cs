namespace CreditSystem.Domain.Abstractions.Events;

public interface IDomainEvent
{
    Guid EventId { get; }
    Guid AggregateId { get; }
    DateTime OccurredAt { get; }
    int Version { get; }
}
