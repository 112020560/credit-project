using CreditSystem.Domain.Abstractions.Events;

namespace CreditSystem.Domain.Aggregates.RevolvingCredit.Events;

public record CreditLineFrozen : DomainEvent
{
    public string Reason { get; init; } = null!;
    public DateTime FrozenAt { get; init; }
}
