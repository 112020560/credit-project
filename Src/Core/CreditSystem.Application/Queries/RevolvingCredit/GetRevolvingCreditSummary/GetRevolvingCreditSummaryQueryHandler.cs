using CreditSystem.Domain.Abstractions.Services;
using MediatR;

namespace CreditSystem.Application.Queries.RevolvingCredit.GetRevolvingCreditSummary;

public class GetRevolvingCreditSummaryQueryHandler : IRequestHandler<GetRevolvingCreditSummaryQuery, RevolvingCreditSummaryResponse?>
{
    private readonly IRevolvingCreditQueryService _queryService;

    public GetRevolvingCreditSummaryQueryHandler(IRevolvingCreditQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<RevolvingCreditSummaryResponse?> Handle(GetRevolvingCreditSummaryQuery request, CancellationToken cancellationToken)
    {
        var summary = await _queryService.GetSummaryAsync(request.CreditLineId, cancellationToken);
        return summary == null ? null : RevolvingCreditSummaryResponse.FromReadModel(summary);
    }
}