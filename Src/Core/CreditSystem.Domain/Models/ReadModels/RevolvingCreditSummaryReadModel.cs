namespace CreditSystem.Domain.Models.ReadModels;

public class RevolvingCreditSummaryReadModel
{
    public Guid CreditLineId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Status { get; set; } = null!;
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AvailableCredit { get; set; }
    public decimal AccruedInterest { get; set; }
    public decimal PendingFees { get; set; }
    public decimal InterestRate { get; set; }
    public decimal MinimumPaymentPercentage { get; set; }
    public decimal MinimumPaymentAmount { get; set; }
    public int BillingCycleDay { get; set; }
    public int GracePeriodDays { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? LastStatementDate { get; set; }
    public DateTime? NextStatementDate { get; set; }
    public DateTime? PaymentDueDate { get; set; }
    public decimal? CurrentMinimumPayment { get; set; }
    public int ConsecutiveMissedPayments { get; set; }
    public DateTime? LastInterestAccrualDate { get; set; }
    public DateTime? FrozenAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string Currency { get; set; } = "USD";
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public decimal TotalOwed => CurrentBalance + AccruedInterest + PendingFees;

    [System.Text.Json.Serialization.JsonIgnore]
    public decimal UtilizationRate => CreditLimit > 0 ? (CurrentBalance / CreditLimit) * 100 : 0;
}