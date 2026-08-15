using MediatR;

namespace CreditSystem.Application.Queries.GetPaymentHistory;

public record GetPaymentHistoryQuery : IRequest<IReadOnlyList<PaymentHistoryResponse>>
{
    public Guid LoanId { get; init; }
}