using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.LoanContract.Events;

public record PaymentMissed : DomainEvent
{
    public int PaymentNumber { get; init; }
    public DateTime DueDate { get; init; }
    public Money AmountDue { get; init; }
    public int DaysOverdue { get; init; }
    public Money LateFeeApplied { get; init; }
}