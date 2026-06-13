namespace CreditSystem.Domain.Models.ReadModels;

public class LoanPortfolioReadModel
{
    public string Id { get; set; } = "global";
    public int TotalLoans { get; set; }
    public int ActiveLoans { get; set; }
    public int DelinquentLoans { get; set; }
    public int DefaultedLoans { get; set; }
    public int PaidOffLoans { get; set; }
    public decimal TotalPrincipal { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalInterestAccrued { get; set; }
    public decimal TotalCollectedPrincipal { get; set; }
    public decimal TotalCollectedInterest { get; set; }
    public decimal TotalCollectedFees { get; set; }
    public decimal? AverageInterestRate { get; set; }
    public decimal? DelinquencyRate { get; set; }
    public decimal? DefaultRate { get; set; }
    public DateTime UpdatedAt { get; set; }

    public void RecalculateRates()
    {
        if (TotalLoans > 0)
        {
            DelinquencyRate = (decimal)DelinquentLoans / TotalLoans;
            DefaultRate = (decimal)DefaultedLoans / TotalLoans;
        }
    }
}