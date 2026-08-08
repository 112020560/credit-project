using CreditSystem.Domain.Abstractions.Events;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.CooperativeMember.Events;

public record MemberRegistered : DomainEvent
{
    public Guid ExternalId { get; init; }
    public string MemberNumber { get; init; } = null!;
    public DateTime JoinedAt { get; init; }
    public MemberShare InitialShares { get; init; } = null!;
}
