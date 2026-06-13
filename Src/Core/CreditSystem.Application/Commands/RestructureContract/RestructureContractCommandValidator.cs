using FluentValidation;

namespace CreditSystem.Application.Commands.RestructureContract;

public class RestructureContractCommandValidator : AbstractValidator<RestructureContractCommand>
{
    public RestructureContractCommandValidator()
    {
        RuleFor(x => x.LoanId)
            .NotEmpty()
            .WithMessage("Loan ID is required");

        RuleFor(x => x.NewInterestRate)
            .InclusiveBetween(0, 50)
            .WithMessage("Interest rate must be between 0% and 50%");

        RuleFor(x => x.NewTermMonths)
            .InclusiveBetween(1, 360)
            .WithMessage("Term must be between 1 and 360 months");

        RuleFor(x => x.ForgiveAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ForgiveAmount.HasValue)
            .WithMessage("Forgive amount cannot be negative");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Restructure reason is required")
            .MinimumLength(10)
            .WithMessage("Reason must be at least 10 characters")
            .MaximumLength(1000)
            .WithMessage("Reason cannot exceed 1000 characters");
    }
}