namespace SharedKernel.Contracts.Payments;

/// <summary>
/// Command to process a revolving credit payment asynchronously.
/// Published to message queue after initial validation and tracking record creation.
/// </summary>
public interface ProcessRevolvingPaymentCommand
{
    /// <summary>
    /// Unique identifier for this payment request.
    /// </summary>
    Guid PaymentId { get; }

    /// <summary>
    /// The revolving credit line to apply the payment to.
    /// </summary>
    Guid CreditLineId { get; }

    /// <summary>
    /// The customer making the payment.
    /// </summary>
    Guid CustomerId { get; }

    /// <summary>
    /// Payment amount.
    /// </summary>
    decimal Amount { get; }

    /// <summary>
    /// Currency code (e.g., "USD", "MXN").
    /// </summary>
    string Currency { get; }

    /// <summary>
    /// Payment method (e.g., "Cash", "BankTransfer", "Card").
    /// </summary>
    string PaymentMethod { get; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    /// When the payment request was submitted.
    /// </summary>
    DateTimeOffset RequestedAt { get; }
}
