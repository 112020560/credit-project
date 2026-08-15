using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.CooperativeMember;

public record MemberState
{
    public static MemberState Initial => new();

    public Guid Id { get; init; }
    public Guid ExternalId { get; init; }
    public string MemberNumber { get; init; } = null!;
    public MemberStatus Status { get; init; }
    public DateTime JoinedAt { get; init; }
    public MemberShare Shares { get; init; } = MemberShare.Zero();
}
