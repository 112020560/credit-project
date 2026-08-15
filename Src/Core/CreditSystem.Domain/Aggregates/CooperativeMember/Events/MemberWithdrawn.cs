using CreditSystem.Domain.Abstractions.Events;

namespace CreditSystem.Domain.Aggregates.CooperativeMember.Events;

public record MemberWithdrawn : DomainEvent
{
    public string Reason { get; init; } = null!;
    public DateTime WithdrawnAt { get; init; }
}
