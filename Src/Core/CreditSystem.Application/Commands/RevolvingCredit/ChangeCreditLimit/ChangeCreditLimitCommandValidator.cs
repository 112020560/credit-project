using FluentValidation;

namespace CreditSystem.Application.Commands.RevolvingCredit.ChangeCreditLimit;

public class ChangeCreditLimitCommandValidator : AbstractValidator<ChangeCreditLimitCommand>
{
    public ChangeCreditLimitCommandValidator()
    {
        RuleFor(x => x.CreditLineId)
            .NotEmpty()
            .WithMessage("Credit line ID is required");

        RuleFor(x => x.NewLimit)
            .GreaterThan(0)
            .WithMessage("New limit must be greater than zero")
            .LessThanOrEqualTo(500000)
            .WithMessage("Credit limit cannot exceed 500,000");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")
            .MinimumLength(10)
            .WithMessage("Reason must be at least 10 characters");
    }
}