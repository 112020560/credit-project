using MediatR;

namespace CreditSystem.Application.Commands.SubmitPayment;

/// <summary>
/// Command to submit a loan payment for asynchronous processing.
/// Returns immediately with a tracking ID.
/// </summary>
public record SubmitPaymentCommand(
    Guid LoanId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string PaymentMethod
) : IRequest<PaymentAcceptedResponse>;
