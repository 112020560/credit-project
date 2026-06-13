namespace CreditSystem.Domain.Aggregates.LoanContract.Events.Base;

public interface IDomainEvent
{
    Guid EventId { get; }
    Guid AggregateId { get; }
    DateTime OccurredAt { get; }
    int Version { get; }
}