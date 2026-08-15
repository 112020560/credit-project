using MediatR;

namespace CreditSystem.Application.Commands.SubmitRevolvingPayment;

/// <summary>
/// Command to submit a revolving credit payment for asynchronous processing.
/// Returns immediately with a tracking ID.
/// </summary>
public record SubmitRevolvingPaymentCommand(
    Guid CreditLineId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string PaymentMethod
) : IRequest<RevolvingPaymentAcceptedResponse>;
