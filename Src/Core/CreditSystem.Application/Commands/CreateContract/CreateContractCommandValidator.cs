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

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(c => AllowedCurrencies.Contains(c))
            .WithMessage($"Currency must be one of: {string.Join(", ", AllowedCurrencies)}");

        RuleFor(x => x.TermMonths)
            .InclusiveBetween(1, 360)
            .WithMessage("Term must be between 1 and 360 months");

        RuleForEach(x => x.Guarantees)
            .ChildRules(g =>
            {
                g.RuleFor(gi => gi.AppraisalValue)
                    .GreaterThan(0)
                    .WithMessage("Guarantee appraisal value must be greater than zero");
                g.RuleFor(gi => gi.CoverageRate)
                    .GreaterThan(0)
                    .LessThanOrEqualTo(1)
                    .WithMessage("Coverage rate must be between 0 (exclusive) and 1 (inclusive)");
                g.RuleFor(gi => gi.Description)
                    .NotEmpty()
                    .WithMessage("Guarantee description is required");
            })
            .When(x => x.Guarantees != null && x.Guarantees.Count > 0);
    }
}