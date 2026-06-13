using MediatR;

namespace CreditSystem.Application.Commands.RevolvingCredit.FreezeCreditLine;

public record FreezeCreditLineCommand : IRequest<FreezeCreditLineResponse>
{
    public Guid CreditLineId { get; init; }
    public string Reason { get; init; } = null!;
}
