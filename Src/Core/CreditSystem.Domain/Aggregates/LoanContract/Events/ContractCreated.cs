using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.LoanContract.Events;

public record ContractCreated : DomainEvent
{
    public Guid CustomerId { get; init; }
    public Money Principal { get; init; }
    public InterestRate InterestRate { get; init; }
    public int TermMonths { get; init; }
    public AmortizationMethod AmortizationMethod { get; init; }
    public PaymentSchedule Schedule { get; init; }
    public Dictionary<string, object> EvaluationMetadata { get; init; }
}