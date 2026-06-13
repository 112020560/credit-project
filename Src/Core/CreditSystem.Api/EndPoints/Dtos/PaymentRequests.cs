namespace CreditSystem.Api.EndPoints.Dtos;

/// <summary>
/// Request to submit a loan payment for async processing.
/// </summary>
public record SubmitPaymentRequest
{
    /// <summary>
    /// The loan to apply the payment to.
    /// </summary>
    public Guid LoanId { get; init; }

    /// <summary>
    /// The customer making the payment.
    /// </summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// Payment amount.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Currency code (default: MXN).
    /// </summary>
    public string? Currency { get; init; }

    /// <summary>
    /// Payment method (Cash, BankTransfer, Card, Check, DirectDebit).
    /// </summary>
    public string PaymentMethod { get; init; } = string.Empty;
}

/// <summary>
/// Request to submit a revolving credit payment for async processing.
/// </summary>
public record SubmitRevolvingPaymentRequest
{
    /// <summary>
    /// The credit line to apply the payment to.
    /// </summary>
    public Guid CreditLineId { get; init; }

    /// <summary>
    /// The customer making the payment.
    /// </summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// Payment amount.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Currency code (default: MXN).
    /// </summary>
    public string? Currency { get; init; }

    /// <summary>
    /// Payment method (Cash, BankTransfer, Card, Check, DirectDebit).
    /// </summary>
    public string PaymentMethod { get; init; } = string.Empty;
}

/// <summary>
/// Request to subscribe to webhook notifications.
/// </summary>
public record SubscribeWebhookRequest
{
    /// <summary>
    /// The customer to subscribe.
    /// </summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// Event type to subscribe to.
    /// Options: payment.completed, payment.failed, payment.rejected,
    /// revolving_payment.completed, revolving_payment.failed, loan.paid_off
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// HTTPS URL to receive webhook notifications.
    /// </summary>
    public string CallbackUrl { get; init; } = string.Empty;

    /// <summary>
    /// Secret key for HMAC signature verification (min 32 chars).
    /// </summary>
    public string SecretKey { get; init; } = string.Empty;
}
