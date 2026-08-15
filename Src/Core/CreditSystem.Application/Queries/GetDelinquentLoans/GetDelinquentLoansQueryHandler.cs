using CreditSystem.Domain.Abstractions.Services;
using MediatR;

namespace CreditSystem.Application.Queries.GetDelinquentLoans;

public class GetDelinquentLoansQueryHandler 
    : IRequestHandler<GetDelinquentLoansQuery, IReadOnlyList<DelinquentLoanResponse>>
{
    private readonly ILoanQueryService _queryService;

    public GetDelinquentLoansQueryHandler(ILoanQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<IReadOnlyList<DelinquentLoanResponse>> Handle(
        GetDelinquentLoansQuery request,
        CancellationToken cancellationToken)
    {
        var loans = await _queryService.GetDelinquentLoansAsync(
            request.MinDaysOverdue,
            request.CollectionStatus,
            cancellationToken);

        return loans
            .Select(DelinquentLoanResponse.FromReadModel)
            .ToList()
            .AsReadOnly();
    }
}