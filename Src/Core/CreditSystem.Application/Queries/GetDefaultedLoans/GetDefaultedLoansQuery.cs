using MediatR;

namespace CreditSystem.Application.Queries.GetDefaultedLoans;

public record GetDefaultedLoansQuery : IRequest<IReadOnlyList<DefaultedLoanResponse>>
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}
