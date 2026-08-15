namespace CreditSystem.Application.Commands.SubscribeWebhook;

/// <summary>
/// Response for webhook subscription command.
/// </summary>
public record SubscribeWebhookResponse
{
    public bool IsSuccess { get; init; }
    public Guid? SubscriptionId { get; init; }
    public string? ErrorMessage { get; init; }

    public static SubscribeWebhookResponse Success(Guid subscriptionId) => new()
    {
        IsSuccess = true,
        SubscriptionId = subscriptionId
    };

    public static SubscribeWebhookResponse Failed(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}
