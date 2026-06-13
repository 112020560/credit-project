namespace CreditSystem.Domain.Aggregates.LoanContract.Events.Base;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid AggregateId { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public int Version { get; init; }
}
