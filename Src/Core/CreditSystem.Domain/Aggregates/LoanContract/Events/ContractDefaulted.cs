using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.LoanContract.Events;

public record ContractDefaulted : DomainEvent
{
    public string Reason { get; init; }
    public int DaysDelinquent { get; init; }
    public Money OutstandingBalance { get; init; }
    public Money AccruedInterest { get; init; }
    public Money TotalOwed { get; init; }
}