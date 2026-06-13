namespace SharedKernel.Contracts.Payments;

/// <summary>
/// Command to process a loan payment asynchronously.
/// Published to message queue after initial validation and tracking record creation.
/// </summary>
public interface ProcessPaymentCommand
{
    /// <summary>
    /// Unique identifier for this payment request.
    /// </summary>
    Guid PaymentId { get; }

    /// <summary>
    /// The loan contract to apply the payment to.
    /// </summary>
    Guid LoanId { get; }

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
