using CreditSystem.Domain.Abstractions.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Application.Queries.GetPaymentStatus;

public class GetPaymentStatusQueryHandler : IRequestHandler<GetPaymentStatusQuery, PaymentStatusResponse?>
{
    private readonly IPaymentTrackingRepository _trackingRepository;
    private readonly ILogger<GetPaymentStatusQueryHandler> _logger;

    public GetPaymentStatusQueryHandler(
        IPaymentTrackingRepository trackingRepository,
        ILogger<GetPaymentStatusQueryHandler> logger)
    {
        _trackingRepository = trackingRepository;
        _logger = logger;
    }

    public async Task<PaymentStatusResponse?> Handle(
        GetPaymentStatusQuery request,
        CancellationToken cancellationToken)
    {
        var tracking = await _trackingRepository.GetByPaymentIdAsync(request.PaymentId, cancellationToken);

        if (tracking == null)
        {
            _logger.LogDebug("Payment {PaymentId} not found", request.PaymentId);
            return null;
        }

        return new PaymentStatusResponse
        {
            PaymentId = tracking.PaymentId,
            LoanId = tracking.LoanId,
            CreditLineId = tracking.CreditLineId,
            CustomerId = tracking.CustomerId,
            Amount = tracking.Amount,
            Currency = tracking.Currency,
            PaymentMethod = tracking.PaymentMethod,
            Status = tracking.Status.ToString().ToUpperInvariant(),
            AcceptedAt = tracking.CreatedAt,
            ProcessedAt = tracking.ProcessedAt,
            PrincipalPaid = tracking.PrincipalPaid,
            InterestPaid = tracking.InterestPaid,
            FeesPaid = tracking.FeesPaid,
            NewBalance = tracking.NewBalance,
            NewAvailableCredit = tracking.NewAvailableCredit,
            IsPaidOff = tracking.IsPaidOff,
            ErrorCode = tracking.ErrorCode,
            ErrorMessage = tracking.ErrorMessage
        };
    }
}
