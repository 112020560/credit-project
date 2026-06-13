using MediatR;

namespace CreditSystem.Application.Commands.RevolvingCredit.ApplyRevolvingPayment;

public record ApplyRevolvingPaymentCommand : IRequest<ApplyRevolvingPaymentResponse>
{
    public Guid CreditLineId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string PaymentMethod { get; init; } = null!;
}