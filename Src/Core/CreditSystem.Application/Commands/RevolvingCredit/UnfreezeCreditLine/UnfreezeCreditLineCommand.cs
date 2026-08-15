using MediatR;

namespace CreditSystem.Application.Commands.RevolvingCredit.UnfreezeCreditLine;

public record UnfreezeCreditLineCommand : IRequest<UnfreezeCreditLineResponse>
{
    public Guid CreditLineId { get; init; }
    public string Reason { get; init; } = null!;
}