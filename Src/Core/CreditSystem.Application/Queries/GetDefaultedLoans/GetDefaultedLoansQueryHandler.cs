using CreditSystem.Domain.Abstractions.Services;
using MediatR;
using Mapster;

namespace CreditSystem.Application.Queries.GetDefaultedLoans;

public class GetDefaultedLoansQueryHandler
    : IRequestHandler<GetDefaultedLoansQuery, IReadOnlyList<DefaultedLoanResponse>>
{
    private readonly ILoanQueryService _queryService;

    public GetDefaultedLoansQueryHandler(ILoanQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<IReadOnlyList<DefaultedLoanResponse>> Handle(
        GetDefaultedLoansQuery request,
        CancellationToken cancellationToken)
    {
        var loans = await _queryService.GetDefaultedLoansAsync(
            request.FromDate,
            request.ToDate,
            cancellationToken);

        var result = loans.Adapt<IReadOnlyList<DefaultedLoanResponse>>();

        return result;
    }
}