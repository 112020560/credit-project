using MediatR;

namespace CreditSystem.Application.Queries.GetPaymentStatus;

/// <summary>
/// Query to get the current status of an asynchronous payment.
/// </summary>
public record GetPaymentStatusQuery(Guid PaymentId) : IRequest<PaymentStatusResponse?>;
