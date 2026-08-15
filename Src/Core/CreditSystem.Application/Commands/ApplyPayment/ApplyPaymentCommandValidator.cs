using FluentValidation;

namespace CreditSystem.Application.Commands.ApplyPayment;

public class ApplyPaymentCommandValidator : AbstractValidator<ApplyPaymentCommand>
{
    private static readonly string[] AllowedMethods = { "ACH", "WIRE", "CHECK", "CARD", "CASH" };
    private static readonly string[] AllowedCurrencies = { "USD", "EUR", "CRC" };

    public ApplyPaymentCommandValidator()
    {
        RuleFor(x => x.LoanId)
            .NotEmpty()
            .WithMessage("Loan ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Payment amount must be greater than zero");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(c => AllowedCurrencies.Contains(c.ToUpperInvariant()))
            .WithMessage($"Currency must be one of: {string.Join(", ", AllowedCurrencies)}");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .WithMessage("Payment method is required")
            .Must(m => AllowedMethods.Contains(m.ToUpperInvariant()))
            .WithMessage($"Payment method must be one of: {string.Join(", ", AllowedMethods)}");
    }
}