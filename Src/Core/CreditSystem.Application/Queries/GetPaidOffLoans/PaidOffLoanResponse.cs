namespace CreditSystem.Application.Queries.GetPaidOffLoans;

public record PaidOffLoanResponse
{
    public Guid LoanId { get; init; }
    public Guid CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public decimal Principal { get; init; }
    public decimal TotalInterestPaid { get; init; }
    public decimal TotalFeesPaid { get; init; }
    public int PaymentsMade { get; init; }
    public int OriginalTermMonths { get; init; }
    public bool EarlyPayoff { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime PaidOffAt { get; init; }
    public int DaysToPayoff { get; init; }
}