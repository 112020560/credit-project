using FluentValidation;

namespace CreditSystem.Application.Commands.FailDisbursement;

public class FailDisbursementCommandValidator : AbstractValidator<FailDisbursementCommand>
{
    public FailDisbursementCommandValidator()
    {
        RuleFor(x => x.LoanId)
            .NotEmpty()
            .WithMessage("Loan ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required");
    }
}
