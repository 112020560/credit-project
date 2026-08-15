using MediatR;

namespace CreditSystem.Application.Commands.RestructureContract;

public record RestructureContractCommand : IRequest<RestructureContractResponse>
{
    public Guid LoanId { get; init; }
    public decimal NewInterestRate { get; init; }
    public int NewTermMonths { get; init; }
    public decimal? ForgiveAmount { get; init; }
    public string Reason { get; init; } = null!;
}
