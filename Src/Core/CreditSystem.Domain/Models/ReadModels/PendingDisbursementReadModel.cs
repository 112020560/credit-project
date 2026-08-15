namespace CreditSystem.Domain.Models.ReadModels;

public class PendingDisbursementReadModel
{
    public Guid LoanId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal Principal { get; set; }
    public string Currency { get; set; } = "USD";
    public string? DisbursementMethod { get; set; }
    public string? DestinationAccount { get; set; }
    public DateTime ApprovedAt { get; set; }
    public DateTime? DisbursementInstructedAt { get; set; }
    public string Status { get; set; } = "Approved";
    public DateTime UpdatedAt { get; set; }
}
