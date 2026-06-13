namespace SharedKernel.Contracts.Payments;

/// <summary>
/// Event published when payment processing fails due to a technical error.
/// May be retried.
/// </summary>
public interface PaymentFailed
{
    /// <summary>
    /// The payment request identifier.
    /// </summary>
    Guid PaymentId { get; }

    /// <summary>
    /// The loan or credit line ID (depending on payment type).
    /// </summary>
    Guid? LoanId { get; }

    /// <summary>
    /// The revolving credit line ID (if applicable).
    /// </summary>
    Guid? CreditLineId { get; }

    /// <summary>
    /// The customer who made the payment.
    /// </summary>
    Guid CustomerId { get; }

    /// <summary>
    /// Human-readable failure reason.
    /// </summary>
    string FailureReason { get; }

    /// <summary>
    /// Machine-readable error code for client handling.
    /// </summary>
    string ErrorCode { get; }

    /// <summary>
    /// When the failure occurred.
    /// </summary>
    DateTimeOffset FailedAt { get; }
}
