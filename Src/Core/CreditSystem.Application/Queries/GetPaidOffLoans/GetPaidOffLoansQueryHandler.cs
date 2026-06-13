using CreditSystem.Domain.Abstractions.Services;
using MediatR;
using Mapster;

namespace CreditSystem.Application.Queries.GetPaidOffLoans;

public class GetPaidOffLoansQueryHandler
    : IRequestHandler<GetPaidOffLoansQuery, IReadOnlyList<PaidOffLoanResponse>>
{
    private readonly ILoanQueryService _queryService;

    public GetPaidOffLoansQueryHandler(ILoanQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<IReadOnlyList<PaidOffLoanResponse>> Handle(
        GetPaidOffLoansQuery request,
        CancellationToken cancellationToken)
    {
        var response = await _queryService.GetPaidOffLoansAsync(
            request.FromDate,
            request.ToDate,
            request.EarlyPayoffOnly,
            cancellationToken);

        return response.Adapt<IReadOnlyList<PaidOffLoanResponse>>();
    }
}