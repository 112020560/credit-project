using FluentValidation;

namespace CreditSystem.Application.Commands.RevolvingCredit.ActivateCreditLine;

public class ActivateCreditLineCommandValidator : AbstractValidator<ActivateCreditLineCommand>
{
    public ActivateCreditLineCommandValidator()
    {
        RuleFor(x => x.CreditLineId)
            .NotEmpty()
            .WithMessage("Credit line ID is required");
    }
}