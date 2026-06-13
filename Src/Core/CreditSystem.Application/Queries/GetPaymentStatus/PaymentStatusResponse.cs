namespace CreditSystem.Application.Queries.GetPaymentStatus;

/// <summary>
/// Response containing the current status of an asynchronous payment.
/// </summary>
public record PaymentStatusResponse
{
    public Guid PaymentId { get; init; }
    public Guid? LoanId { get; init; }
    public Guid? CreditLineId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset AcceptedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; init; }

    // Completed payment details
    public decimal? PrincipalPaid { get; init; }
    public decimal? InterestPaid { get; init; }
    public decimal? FeesPaid { get; init; }
    public decimal? NewBalance { get; init; }
    public decimal? NewAvailableCredit { get; init; }
    public bool? IsPaidOff { get; init; }

    // Error details (for failed/rejected)
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}
