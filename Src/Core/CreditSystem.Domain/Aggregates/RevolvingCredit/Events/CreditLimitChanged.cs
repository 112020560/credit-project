using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.RevolvingCredit.Events;

public record CreditLimitChanged : DomainEvent
{
    public Money PreviousLimit { get; init; } = null!;
    public Money NewLimit { get; init; } = null!;
    public string Reason { get; init; } = null!;
}