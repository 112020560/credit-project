using CreditSystem.Domain.Abstractions.Events;

namespace CreditSystem.Domain.Aggregates.LoanContract.Events;

public record DisbursementFailed : DomainEvent
{
    public string Reason { get; init; } = null!;
    public DateTime FailedAt { get; init; }
}
