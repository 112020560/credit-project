using System.Text.Json.Serialization;

namespace CreditSystem.Domain.Models.ReadModels;

public class LoanSummaryReadModel
{
    public Guid LoanId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal Principal { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AccruedInterest { get; set; }
    public decimal TotalFees { get; set; }
    public decimal InterestRate { get; set; }
    public int TermMonths { get; set; }
    public string Status { get; set; } = null!;
    public int PaymentsMade { get; set; }
    public int PaymentsMissed { get; set; }
    public DateTime? NextPaymentDate { get; set; }
    public decimal? NextPaymentAmount { get; set; }
    public DateTime? DisbursedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastPaymentAt { get; set; }
    public DateTime? DefaultedAt { get; set; }
    public DateTime? PaidOffAt { get; set; }
    public int Version { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Computed
    [JsonIgnore]
    public decimal TotalOwed => CurrentBalance + AccruedInterest + TotalFees;
    [JsonIgnore]
    public bool IsDelinquent => PaymentsMissed > 0;
}