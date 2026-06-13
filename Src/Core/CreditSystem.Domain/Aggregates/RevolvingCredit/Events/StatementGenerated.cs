using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.RevolvingCredit.Events;

public record StatementGenerated : DomainEvent
{
    public Guid StatementId { get; init; }
    public DateTime StatementDate { get; init; }
    public DateTime DueDate { get; init; }
    public Money PreviousBalance { get; init; } = null!;
    public Money Purchases { get; init; } = null!;
    public Money Payments { get; init; } = null!;
    public Money InterestCharged { get; init; } = null!;
    public Money FeesCharged { get; init; } = null!;
    public Money NewBalance { get; init; } = null!;
    public Money MinimumPayment { get; init; } = null!;
}