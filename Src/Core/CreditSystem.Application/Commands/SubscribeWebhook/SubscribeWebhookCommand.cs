using MediatR;

namespace CreditSystem.Application.Commands.SubscribeWebhook;

/// <summary>
/// Command to subscribe to webhook notifications for payment events.
/// </summary>
public record SubscribeWebhookCommand(
    Guid CustomerId,
    string EventType,
    string CallbackUrl,
    string SecretKey
) : IRequest<SubscribeWebhookResponse>;
