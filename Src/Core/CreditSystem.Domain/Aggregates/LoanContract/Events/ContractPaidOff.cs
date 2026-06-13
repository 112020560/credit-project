using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.LoanContract.Events;

public record ContractPaidOff : DomainEvent
{
    public Money FinalPayment { get; init; }
    public Money TotalPrincipalPaid { get; init; }
    public Money TotalInterestPaid { get; init; }
    public Money TotalFeesPaid { get; init; }
    public DateTime PaidOffAt { get; init; }
    public bool EarlyPayoff { get; init; }
}