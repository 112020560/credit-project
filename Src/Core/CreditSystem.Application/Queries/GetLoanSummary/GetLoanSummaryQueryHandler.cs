using CreditSystem.Domain.Abstractions.Services;
using MediatR;

namespace CreditSystem.Application.Queries.GetLoanSummary;

public class GetLoanSummaryQueryHandler : IRequestHandler<GetLoanSummaryQuery, LoanSummaryResponse?>
{
    private readonly ILoanQueryService _queryService;

    public GetLoanSummaryQueryHandler(ILoanQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<LoanSummaryResponse?> Handle(
        GetLoanSummaryQuery request, 
        CancellationToken cancellationToken)
    {
        var model = await _queryService.GetLoanSummaryAsync(request.LoanId, cancellationToken);
        
        return model == null ? null : LoanSummaryResponse.FromReadModel(model);
    }
}