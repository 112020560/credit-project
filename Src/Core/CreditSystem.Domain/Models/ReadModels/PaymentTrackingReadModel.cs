namespace CreditSystem.Domain.Models.ReadModels;

/// <summary>
/// Read model for tracking asynchronous payment processing status.
/// Stored in rm_payment_tracking table.
/// </summary>
public class PaymentTrackingReadModel
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid? LoanId { get; set; }
    public Guid? CreditLineId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MXN";
    public string PaymentMethod { get; set; } = string.Empty;
    public PaymentTrackingStatus Status { get; set; } = PaymentTrackingStatus.Pending;
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public decimal? PrincipalPaid { get; set; }
    public decimal? InterestPaid { get; set; }
    public decimal? FeesPaid { get; set; }
    public decimal? NewBalance { get; set; }
    public decimal? NewAvailableCredit { get; set; }
    public bool? IsPaidOff { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public Guid CorrelationId { get; set; }
}

public enum PaymentTrackingStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Rejected
}
