using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.RevolvingCredit.Events;

public record CreditLineCreated : DomainEvent
{
    public Guid CustomerId { get; init; }
    public Money CreditLimit { get; init; } = null!;
    public InterestRate InterestRate { get; init; } = null!;
    public decimal MinimumPaymentPercentage { get; init; }
    public Money MinimumPaymentAmount { get; init; } = null!;
    public int BillingCycleDay { get; init; }
    public int GracePeriodDays { get; init; }
}