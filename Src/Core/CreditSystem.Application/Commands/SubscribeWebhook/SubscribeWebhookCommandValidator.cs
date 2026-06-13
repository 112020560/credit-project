using CreditSystem.Domain.Entities;
using FluentValidation;

namespace CreditSystem.Application.Commands.SubscribeWebhook;

public class SubscribeWebhookCommandValidator : AbstractValidator<SubscribeWebhookCommand>
{
    private static readonly string[] ValidEventTypes =
    {
        WebhookEventTypes.PaymentCompleted,
        WebhookEventTypes.PaymentFailed,
        WebhookEventTypes.PaymentRejected,
        WebhookEventTypes.RevolvingPaymentCompleted,
        WebhookEventTypes.RevolvingPaymentFailed,
        WebhookEventTypes.LoanPaidOff
    };

    public SubscribeWebhookCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.EventType)
            .NotEmpty()
            .WithMessage("Event type is required")
            .Must(e => ValidEventTypes.Contains(e))
            .WithMessage($"Event type must be one of: {string.Join(", ", ValidEventTypes)}");

        RuleFor(x => x.CallbackUrl)
            .NotEmpty()
            .WithMessage("Callback URL is required")
            .Must(BeValidHttpsUrl)
            .WithMessage("Callback URL must be a valid HTTPS URL");

        RuleFor(x => x.SecretKey)
            .NotEmpty()
            .WithMessage("Secret key is required")
            .MinimumLength(32)
            .WithMessage("Secret key must be at least 32 characters");
    }

    private static bool BeValidHttpsUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               uri.Scheme == Uri.UriSchemeHttps;
    }
}
