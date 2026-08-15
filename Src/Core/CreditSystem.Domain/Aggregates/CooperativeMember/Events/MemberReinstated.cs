using CreditSystem.Domain.Abstractions.Events;

namespace CreditSystem.Domain.Aggregates.CooperativeMember.Events;

public record MemberReinstated : DomainEvent
{
    public string Reason { get; init; } = null!;
    public DateTime ReinstatedAt { get; init; }
}
