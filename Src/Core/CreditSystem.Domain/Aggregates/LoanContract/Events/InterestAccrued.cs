using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.LoanContract.Events;

public record InterestAccrued : DomainEvent
{
    public Money Amount { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public Money PrincipalBalance { get; init; }
    public InterestRate RateApplied { get; init; }
}
