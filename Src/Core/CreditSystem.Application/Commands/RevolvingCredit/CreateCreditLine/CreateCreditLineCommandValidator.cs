using FluentValidation;

namespace CreditSystem.Application.Commands.RevolvingCredit.CreateCreditLine;

public class CreateCreditLineCommandValidator : AbstractValidator<CreateCreditLineCommand>
{
    public CreateCreditLineCommandValidator()
    {
        RuleFor(x => x.ExternalCustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.CreditLimit)
            .GreaterThan(0)
            .WithMessage("Credit limit must be greater than zero")
            .LessThanOrEqualTo(100000)
            .WithMessage("Credit limit cannot exceed 100,000");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(c => new[] { "USD", "EUR", "CRC" }.Contains(c.ToUpperInvariant()))
            .WithMessage("Currency must be USD, EUR, or CRC");

        RuleFor(x => x.MinimumPaymentPercentage)
            .InclusiveBetween(1, 100)
            .WithMessage("Minimum payment percentage must be between 1% and 100%");

        RuleFor(x => x.MinimumPaymentAmount)
            .GreaterThan(0)
            .WithMessage("Minimum payment amount must be greater than zero");

        RuleFor(x => x.BillingCycleDay)
            .InclusiveBetween(1, 28)
            .WithMessage("Billing cycle day must be between 1 and 28");

        RuleFor(x => x.GracePeriodDays)
            .InclusiveBetween(10, 30)
            .WithMessage("Grace period must be between 10 and 30 days");
    }
}