namespace CreditSystem.Domain.Models.Documents;

public class BalanceLetterData
{
    public Guid LoanId { get; init; }
    public string LoanNumber { get; init; } = null!;
    public string CustomerName { get; init; } = null!;
    public string Currency { get; init; } = null!;
    public DateTime DisbursementDate { get; init; }
    public decimal OriginalAmount { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal InterestRate { get; init; }
    public string RateType { get; init; } = "Fixed";
    public decimal? Spread { get; init; }
    public string? ReferenceRateId { get; init; }
    public DateTime? NextPaymentDate { get; init; }
    public decimal? NextPaymentAmount { get; init; }
    public DateTime? MaturityDate { get; init; }
    public string Status { get; init; } = null!;
    public DateTime IssuedAt { get; init; }
}
