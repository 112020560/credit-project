using MediatR;

namespace CreditSystem.Application.Commands.RevolvingCredit.ActivateCreditLine;

public record ActivateCreditLineCommand : IRequest<ActivateCreditLineResponse>
{
    public Guid CreditLineId { get; init; }
}