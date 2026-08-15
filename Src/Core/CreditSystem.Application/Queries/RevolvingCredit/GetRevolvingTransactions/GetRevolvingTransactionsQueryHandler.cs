using CreditSystem.Domain.Abstractions.Services;
using MediatR;

namespace CreditSystem.Application.Queries.RevolvingCredit.GetRevolvingTransactions;

public class GetRevolvingTransactionsQueryHandler : IRequestHandler<GetRevolvingTransactionsQuery, IReadOnlyList<RevolvingTransactionResponse>>
{
    private readonly IRevolvingCreditQueryService _queryService;

    public GetRevolvingTransactionsQueryHandler(IRevolvingCreditQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<IReadOnlyList<RevolvingTransactionResponse>> Handle(GetRevolvingTransactionsQuery request, CancellationToken cancellationToken)
    {
        var transactions = await _queryService.GetTransactionsAsync(request.CreditLineId, request.Limit, cancellationToken);
        return transactions.Select(RevolvingTransactionResponse.FromReadModel).ToList().AsReadOnly();
    }
}