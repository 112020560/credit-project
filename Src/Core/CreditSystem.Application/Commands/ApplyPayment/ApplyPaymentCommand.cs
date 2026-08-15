using MediatR;

namespace CreditSystem.Application.Commands.ApplyPayment;

public record ApplyPaymentCommand : IRequest<ApplyPaymentResponse>
{
    public Guid LoanId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string PaymentMethod { get; init; } = null!;  // ACH, WIRE, CHECK, CARD, CASH
    public string? ReferenceNumber { get; init; }
}
