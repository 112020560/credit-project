using FluentValidation;

namespace CreditSystem.Application.Commands.ConfirmDisbursement;

public class ConfirmDisbursementCommandValidator : AbstractValidator<ConfirmDisbursementCommand>
{
    public ConfirmDisbursementCommandValidator()
    {
        RuleFor(x => x.LoanId)
            .NotEmpty()
            .WithMessage("Loan ID is required");

        RuleFor(x => x.ConfirmedBy)
            .NotEmpty()
            .WithMessage("ConfirmedBy is required");
    }
}
