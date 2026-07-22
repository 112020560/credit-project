using CreditSystem.Domain.Abstractions.Events;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.LoanContract.Events;

public record ContractApproved : DomainEvent
{
    public Guid CustomerId { get; init; }
    public InterestRate ApprovedRate { get; init; } = null!;
    public Money ApprovedPrincipal { get; init; } = null!;
    public Dictionary<string, object> EvaluationMetadata { get; init; } = new();
}
