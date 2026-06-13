using FluentValidation;

namespace CreditSystem.Application.Commands.RevolvingCredit.ApplyRevolvingPayment;

public class ApplyRevolvingPaymentCommandValidator : AbstractValidator<ApplyRevolvingPaymentCommand>
{
    private static readonly string[] AllowedMethods = { "ACH", "WIRE", "CHECK", "CARD", "CASH" };

    public ApplyRevolvingPaymentCommandValidator()
    {
        RuleFor(x => x.CreditLineId)
            .NotEmpty()
            .WithMessage("Credit line ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Payment amount must be greater than zero");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .WithMessage("Payment method is required")
            .Must(m => AllowedMethods.Contains(m.ToUpperInvariant()))
            .WithMessage($"Payment method must be one of: {string.Join(", ", AllowedMethods)}");
    }
}