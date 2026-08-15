using MediatR;

namespace CreditSystem.Application.Commands.RevolvingCredit.CloseCreditLine;

public record CloseCreditLineCommand : IRequest<CloseCreditLineResponse>
{
    public Guid CreditLineId { get; init; }
    public string Reason { get; init; } = null!;
}