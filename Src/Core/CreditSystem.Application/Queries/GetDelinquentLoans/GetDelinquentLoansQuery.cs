using MediatR;

namespace CreditSystem.Application.Queries.GetDelinquentLoans;

public record GetDelinquentLoansQuery : IRequest<IReadOnlyList<DelinquentLoanResponse>>
{
    public int? MinDaysOverdue { get; init; }
    public string? CollectionStatus { get; init; }
}