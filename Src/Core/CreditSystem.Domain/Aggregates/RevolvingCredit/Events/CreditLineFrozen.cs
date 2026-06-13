using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;

namespace CreditSystem.Domain.Aggregates.RevolvingCredit.Events;

public record CreditLineFrozen : DomainEvent
{
    public string Reason { get; init; } = null!;
    public DateTime FrozenAt { get; init; }
}
