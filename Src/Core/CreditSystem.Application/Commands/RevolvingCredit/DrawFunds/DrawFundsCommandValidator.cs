using FluentValidation;

namespace CreditSystem.Application.Commands.RevolvingCredit.DrawFunds;

public class DrawFundsCommandValidator : AbstractValidator<DrawFundsCommand>
{
    public DrawFundsCommandValidator()
    {
        RuleFor(x => x.CreditLineId)
            .NotEmpty()
            .WithMessage("Credit line ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters");
    }
}