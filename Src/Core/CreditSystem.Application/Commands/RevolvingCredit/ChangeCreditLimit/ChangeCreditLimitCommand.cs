using MediatR;

namespace CreditSystem.Application.Commands.RevolvingCredit.ChangeCreditLimit;

public record ChangeCreditLimitCommand : IRequest<ChangeCreditLimitResponse>
{
    public Guid CreditLineId { get; init; }
    public decimal NewLimit { get; init; }
    public string Currency { get; init; } = "USD";
    public string Reason { get; init; } = null!;
}