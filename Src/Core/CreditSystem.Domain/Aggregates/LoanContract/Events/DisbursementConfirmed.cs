using CreditSystem.Domain.Abstractions.Events;

namespace CreditSystem.Domain.Aggregates.LoanContract.Events;

public record DisbursementConfirmed : DomainEvent
{
    public string ConfirmedBy { get; init; } = null!;
    public DateTime DisbursedAt { get; init; }
}
