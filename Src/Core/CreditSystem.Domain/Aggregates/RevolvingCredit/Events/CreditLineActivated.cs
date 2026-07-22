using CreditSystem.Domain.Abstractions.Events;

namespace CreditSystem.Domain.Aggregates.RevolvingCredit.Events;

public record CreditLineActivated : DomainEvent
{
    public DateTime ActivatedAt { get; init; }
}