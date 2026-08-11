namespace CreditSystem.Domain.Models.Documents;

public class AmortizationTableData
{
    public Guid LoanId { get; init; }
    public string LoanNumber { get; init; } = null!;
    public string CustomerName { get; init; } = null!;
    public string Currency { get; init; } = null!;
    public decimal OriginalAmount { get; init; }
    public decimal InterestRate { get; init; }
    public int TermMonths { get; init; }
    public DateTime GeneratedAt { get; init; }
    public IReadOnlyList<AmortizationRowData> Rows { get; init; } = [];
    public decimal TotalPayment { get; init; }
    public decimal TotalInterest { get; init; }
    public decimal TotalPrincipal { get; init; }
}
