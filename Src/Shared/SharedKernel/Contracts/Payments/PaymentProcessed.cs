namespace SharedKernel.Contracts.Payments;

/// <summary>
/// Event published when a loan payment has been successfully processed.
/// </summary>
public interface PaymentProcessed
{
    /// <summary>
    /// The payment request identifier.
    /// </summary>
    Guid PaymentId { get; }

    /// <summary>
    /// The loan contract that received the payment.
    /// </summary>
    Guid LoanId { get; }

    /// <summary>
    /// The customer who made the payment.
    /// </summary>
    Guid CustomerId { get; }

    /// <summary>
    /// Total amount applied to the loan.
    /// </summary>
    decimal TotalApplied { get; }

    /// <summary>
    /// Currency code.
    /// </summary>
    string Currency { get; }

    /// <summary>
    /// Amount applied to principal.
    /// </summary>
    decimal PrincipalPaid { get; }

    /// <summary>
    /// Amount applied to interest.
    /// </summary>
    decimal InterestPaid { get; }

    /// <summary>
    /// Amount applied to fees.
    /// </summary>
    decimal FeesPaid { get; }

    /// <summary>
    /// Remaining loan balance after payment.
    /// </summary>
    decimal NewBalance { get; }

    /// <summary>
    /// Whether the loan is fully paid off.
    /// </summary>
    bool IsPaidOff { get; }

    /// <summary>
    /// The payment method used.
    /// </summary>
    string PaymentMethod { get; }

    /// <summary>
    /// When the payment was processed.
    /// </summary>
    DateTimeOffset ProcessedAt { get; }
}
