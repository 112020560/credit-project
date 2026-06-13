using FluentValidation;

namespace CreditSystem.Application.Commands.DisburseLoan;

public class DisburseLoanCommandValidator : AbstractValidator<DisburseLoanCommand>
{
    private static readonly string[] AllowedMethods = { "WIRE", "ACH", "CHECK" };

    public DisburseLoanCommandValidator()
    {
        RuleFor(x => x.LoanId)
            .NotEmpty()
            .WithMessage("Loan ID is required");

        RuleFor(x => x.DisbursementMethod)
            .NotEmpty()
            .WithMessage("Disbursement method is required")
            .Must(m => AllowedMethods.Contains(m.ToUpperInvariant()))
            .WithMessage($"Disbursement method must be one of: {string.Join(", ", AllowedMethods)}");

        RuleFor(x => x.DestinationAccount)
            .NotEmpty()
            .WithMessage("Destination account is required")
            .MinimumLength(5)
            .WithMessage("Destination account must be at least 5 characters");
    }
}