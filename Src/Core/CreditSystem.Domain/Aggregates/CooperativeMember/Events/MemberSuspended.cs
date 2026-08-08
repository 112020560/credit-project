using CreditSystem.Domain.Abstractions.Events;

namespace CreditSystem.Domain.Aggregates.CooperativeMember.Events;

public record MemberSuspended : DomainEvent
{
    public string Reason { get; init; } = null!;
    public DateTime SuspendedAt { get; init; }
}
