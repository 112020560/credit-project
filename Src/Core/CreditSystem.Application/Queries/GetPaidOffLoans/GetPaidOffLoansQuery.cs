using MediatR;

namespace CreditSystem.Application.Queries.GetPaidOffLoans;

public record GetPaidOffLoansQuery : IRequest<IReadOnlyList<PaidOffLoanResponse>>
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public bool? EarlyPayoffOnly { get; init; }
}