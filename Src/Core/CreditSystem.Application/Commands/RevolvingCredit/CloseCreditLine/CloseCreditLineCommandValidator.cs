using FluentValidation;

namespace CreditSystem.Application.Commands.RevolvingCredit.CloseCreditLine;

public class CloseCreditLineCommandValidator : AbstractValidator<CloseCreditLineCommand>
{
    public CloseCreditLineCommandValidator()
    {
        RuleFor(x => x.CreditLineId)
            .NotEmpty()
            .WithMessage("Credit line ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")
            .MinimumLength(10)
            .WithMessage("Reason must be at least 10 characters");
    }
}