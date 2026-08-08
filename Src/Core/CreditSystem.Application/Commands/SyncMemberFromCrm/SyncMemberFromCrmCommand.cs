using MediatR;

namespace CreditSystem.Application.Commands.SyncMemberFromCrm;

public record SyncMemberFromCrmCommand : IRequest
{
    public Guid ExternalId { get; init; }
    public string MemberNumber { get; init; } = null!;
    public string Status { get; init; } = null!;
    public DateTime JoinedAt { get; init; }
    public decimal TotalSharesAmount { get; init; }
    public string SharesCurrency { get; init; } = "CRC";
    public int NumberOfContributions { get; init; }
    public DateTime? LastContributionDate { get; init; }
}
