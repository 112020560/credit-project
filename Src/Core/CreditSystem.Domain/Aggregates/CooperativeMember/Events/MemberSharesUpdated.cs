using CreditSystem.Domain.Abstractions.Events;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.CooperativeMember.Events;

public record MemberSharesUpdated : DomainEvent
{
    public MemberShare NewShares { get; init; } = null!;
    public DateTime UpdatedAt { get; init; }
}
