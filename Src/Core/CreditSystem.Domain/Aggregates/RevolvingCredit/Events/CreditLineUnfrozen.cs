using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;

namespace CreditSystem.Domain.Aggregates.RevolvingCredit.Events;

public record CreditLineUnfrozen : DomainEvent
{
    public string Reason { get; init; } = null!;
    public DateTime UnfrozenAt { get; init; }
}
