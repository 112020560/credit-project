using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;

namespace CreditSystem.Domain.Aggregates.RevolvingCredit.Events;

public record CreditLineActivated : DomainEvent
{
    public DateTime ActivatedAt { get; init; }
}