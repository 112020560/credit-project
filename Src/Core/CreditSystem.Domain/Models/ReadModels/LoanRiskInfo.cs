namespace CreditSystem.Domain.Models.ReadModels;

public record LoanRiskInfo
{
    public Guid LoanId { get; init; }
    public decimal CurrentBalance { get; init; }
    public int DaysOverdue { get; init; }
    public string? CurrentRiskCategory { get; init; }
}
