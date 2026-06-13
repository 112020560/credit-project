using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.RevolvingCredit.Events;

public record FundsDrawn : DomainEvent
{
    public Guid DrawId { get; init; }
    public Money Amount { get; init; } = null!;
    public string Description { get; init; } = null!;
    public Money NewBalance { get; init; } = null!;
    public Money AvailableCredit { get; init; } = null!;
    public DateTime DrawnAt { get; init; }
}