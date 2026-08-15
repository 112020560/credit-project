using MediatR;

namespace CreditSystem.Application.Commands.EnrollMember;

public record EnrollMemberCommand : IRequest<EnrollMemberResponse>
{
    public Guid ExternalCustomerId { get; init; }
    public DateTime JoinedAt { get; init; }
    public decimal InitialSharesAmount { get; init; }
    public string SharesCurrency { get; init; } = "CRC";
}
