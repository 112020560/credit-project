using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.RevolvingCredit.Events;

public record RevolvingInterestAccrued : DomainEvent
{
    public Money Amount { get; init; } = null!;
    public Money AverageBalance { get; init; } = null!;
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
}
