namespace SharedKernel.Contracts.Payments;

/// <summary>
/// Event published when a revolving credit payment has been successfully processed.
/// </summary>
public interface RevolvingPaymentProcessed
{
    /// <summary>
    /// The payment request identifier.
    /// </summary>
    Guid PaymentId { get; }

    /// <summary>
    /// The revolving credit line that received the payment.
    /// </summary>
    Guid CreditLineId { get; }

    /// <summary>
    /// The customer who made the payment.
    /// </summary>
    Guid CustomerId { get; }

    /// <summary>
    /// Total amount applied.
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
    /// New available credit after payment.
    /// </summary>
    decimal NewAvailableCredit { get; }

    /// <summary>
    /// New used credit after payment.
    /// </summary>
    decimal NewUsedCredit { get; }

    /// <summary>
    /// The payment method used.
    /// </summary>
    string PaymentMethod { get; }

    /// <summary>
    /// When the payment was processed.
    /// </summary>
    DateTimeOffset ProcessedAt { get; }
}
