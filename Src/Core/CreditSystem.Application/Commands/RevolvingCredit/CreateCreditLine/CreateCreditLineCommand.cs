using MediatR;

namespace CreditSystem.Application.Commands.RevolvingCredit.CreateCreditLine;

public record CreateCreditLineCommand : IRequest<CreateCreditLineResponse>
{
    public Guid ExternalCustomerId { get; init; }
    public decimal CreditLimit { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal? InterestRate { get; init; }
    public decimal MinimumPaymentPercentage { get; init; } = 5;
    public decimal MinimumPaymentAmount { get; init; } = 25;
    public int BillingCycleDay { get; init; } = 15;
    public int GracePeriodDays { get; init; } = 20;
}
