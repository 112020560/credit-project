using CreditSystem.Domain.Abstractions.Services;
using MediatR;

namespace CreditSystem.Application.Queries.GetPaymentHistory;

public class GetPaymentHistoryQueryHandler : IRequestHandler<GetPaymentHistoryQuery, IReadOnlyList<PaymentHistoryResponse>>
{
    private readonly ILoanQueryService _queryService;

    public GetPaymentHistoryQueryHandler(ILoanQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<IReadOnlyList<PaymentHistoryResponse>> Handle(
        GetPaymentHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var payments = await _queryService.GetPaymentHistoryAsync(request.LoanId, cancellationToken);
        
        return payments
            .Select(PaymentHistoryResponse.FromReadModel)
            .ToList()
            .AsReadOnly();
    }
}