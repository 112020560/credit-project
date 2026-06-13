using FluentValidation;

namespace CreditSystem.Application.Commands.PayoffContract;

public class PayoffContractCommandValidator : AbstractValidator<PayoffContractCommand>
{
    private static readonly string[] AllowedMethods = { "ACH", "WIRE", "CHECK", "CARD", "CASH" };

    public PayoffContractCommandValidator()
    {
        RuleFor(x => x.LoanId)
            .NotEmpty()
            .WithMessage("Loan ID is required");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .WithMessage("Payment method is required")
            .Must(m => AllowedMethods.Contains(m.ToUpperInvariant()))
            .WithMessage($"Payment method must be one of: {string.Join(", ", AllowedMethods)}");
    }
}