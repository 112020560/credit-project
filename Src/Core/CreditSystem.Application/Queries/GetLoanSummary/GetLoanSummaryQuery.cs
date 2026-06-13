using MediatR;

namespace CreditSystem.Application.Queries.GetLoanSummary;

public record GetLoanSummaryQuery : IRequest<LoanSummaryResponse?>
{
    public Guid LoanId { get; init; }
}