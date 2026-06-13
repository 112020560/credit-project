namespace CreditSystem.Domain.Entities;

/// <summary>
/// Represents a webhook subscription for a customer to receive event notifications.
/// </summary>
public class WebhookSubscription
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Represents a webhook delivery attempt.
/// </summary>
public class WebhookDelivery
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid PaymentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public WebhookDeliveryStatus Status { get; set; } = WebhookDeliveryStatus.Pending;
    public int? HttpStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
}

public enum WebhookDeliveryStatus
{
    Pending,
    Delivered,
    Failed
}

/// <summary>
/// Supported webhook event types.
/// </summary>
public static class WebhookEventTypes
{
    public const string PaymentCompleted = "payment.completed";
    public const string PaymentFailed = "payment.failed";
    public const string PaymentRejected = "payment.rejected";
    public const string RevolvingPaymentCompleted = "revolving_payment.completed";
    public const string RevolvingPaymentFailed = "revolving_payment.failed";
    public const string LoanPaidOff = "loan.paid_off";
}
