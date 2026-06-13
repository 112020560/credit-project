using FluentValidation;

namespace CreditSystem.Application.Commands.SubmitRevolvingPayment;

public class SubmitRevolvingPaymentCommandValidator : AbstractValidator<SubmitRevolvingPaymentCommand>
{
    private static readonly string[] ValidPaymentMethods =
        { "Cash", "BankTransfer", "Card", "Check", "DirectDebit" };

    private static readonly string[] ValidCurrencies = { "MXN", "USD" };

    public SubmitRevolvingPaymentCommandValidator()
    {
        RuleFor(x => x.CreditLineId)
            .NotEmpty()
            .WithMessage("Credit line ID is required");

        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Must(c => ValidCurrencies.Contains(c, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Currency must be one of: {string.Join(", ", ValidCurrencies)}");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .WithMessage("Payment method is required")
            .Must(m => ValidPaymentMethods.Contains(m, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Payment method must be one of: {string.Join(", ", ValidPaymentMethods)}");
    }
}
