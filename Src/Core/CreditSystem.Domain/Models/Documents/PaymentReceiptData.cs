namespace CreditSystem.Domain.Models.Documents;

public class PaymentReceiptData
{
    public Guid LoanId { get; init; }
    public string LoanNumber { get; init; } = null!;
    public string CustomerName { get; init; } = null!;
    public string Currency { get; init; } = null!;
    public DateTime PaymentDate { get; init; }
    public decimal PrincipalApplied { get; init; }
    public decimal InterestApplied { get; init; }
    public decimal? FeesApplied { get; init; }
    public decimal TotalPaid { get; init; }
    public decimal RemainingBalance { get; init; }
    public decimal? SocialCapitalContributed { get; init; }
}
