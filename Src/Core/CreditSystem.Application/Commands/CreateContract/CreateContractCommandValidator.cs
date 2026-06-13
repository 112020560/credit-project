using FluentValidation;

namespace CreditSystem.Application.Commands.CreateContract;

public class CreateContractCommandValidator : AbstractValidator<CreateContractCommand>
{
    private static readonly string[] AllowedCurrencies = { "USD", "EUR", "CRC" };

    public CreateContractCommandValidator()
    {
        RuleFor(x => x.ExternalCustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Amount cannot exceed 1,000,000");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(c => AllowedCurrencies.Contains(c))
            .WithMessage($"Currency must be one of: {string.Join(", ", AllowedCurrencies)}");

        RuleFor(x => x.TermMonths)
            .InclusiveBetween(1, 360)
            .WithMessage("Term must be between 1 and 360 months");

        RuleFor(x => x.CollateralValue)
            .GreaterThan(0)
            .When(x => x.CollateralValue.HasValue)
            .WithMessage("Collateral value must be greater than zero when provided");
    }
}