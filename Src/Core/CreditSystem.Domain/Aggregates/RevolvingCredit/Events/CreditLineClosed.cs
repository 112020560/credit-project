using CreditSystem.Domain.Abstractions.Events;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.RevolvingCredit.Events;

public record CreditLineClosed : DomainEvent
{
    public string Reason { get; init; } = null!;
    public Money FinalBalance { get; init; } = null!;
    public DateTime ClosedAt { get; init; }
}