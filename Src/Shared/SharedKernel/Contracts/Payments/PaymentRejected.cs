namespace SharedKernel.Contracts.Payments;

/// <summary>
/// Event published when payment is rejected due to business rules.
/// Should not be retried without changes.
/// </summary>
public interface PaymentRejected
{
    /// <summary>
    /// The payment request identifier.
    /// </summary>
    Guid PaymentId { get; }

    /// <summary>
    /// The loan ID (if applicable).
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
    /// Human-readable rejection reason.
    /// </summary>
    string RejectionReason { get; }

    /// <summary>
    /// Machine-readable rejection code for client handling.
    /// Examples: "LOAN_NOT_FOUND", "LOAN_NOT_ACTIVE", "AMOUNT_EXCEEDS_BALANCE", "INVALID_PAYMENT_METHOD"
    /// </summary>
    string RejectionCode { get; }

    /// <summary>
    /// When the rejection occurred.
    /// </summary>
    DateTimeOffset RejectedAt { get; }
}
