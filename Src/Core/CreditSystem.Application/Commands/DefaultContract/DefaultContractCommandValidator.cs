using FluentValidation;

namespace CreditSystem.Application.Commands.DefaultContract;

public class DefaultContractCommandValidator : AbstractValidator<DefaultContractCommand>
{
    public DefaultContractCommandValidator()
    {
        RuleFor(x => x.LoanId)
            .NotEmpty()
            .WithMessage("Loan ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Default reason is required")
            .MinimumLength(10)
            .WithMessage("Reason must be at least 10 characters")
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters");
    }
}