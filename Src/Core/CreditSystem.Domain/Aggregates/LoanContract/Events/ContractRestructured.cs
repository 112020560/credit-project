using CreditSystem.Domain.Abstractions.Events;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.LoanContract.Events;

public record ContractRestructured : DomainEvent
{
    public InterestRate NewRate { get; init; }
    public int NewTermMonths { get; init; }
    public PaymentSchedule NewSchedule { get; init; }
    public Money ForgiveAmount { get; init; }
    public string RestructureReason { get; init; }
}
