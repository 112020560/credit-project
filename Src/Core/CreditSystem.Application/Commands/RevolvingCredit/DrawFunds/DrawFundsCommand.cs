using MediatR;

namespace CreditSystem.Application.Commands.RevolvingCredit.DrawFunds;

public record DrawFundsCommand : IRequest<DrawFundsResponse>
{
    public Guid CreditLineId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string Description { get; init; } = null!;
}