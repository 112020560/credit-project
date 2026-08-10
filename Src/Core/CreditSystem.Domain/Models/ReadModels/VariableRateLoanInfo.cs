namespace CreditSystem.Domain.Models.ReadModels;

public class VariableRateLoanInfo
{
    public Guid LoanId { get; init; }
    public decimal CurrentRate { get; init; }
    public decimal Spread { get; init; }
    public string ReferenceRateId { get; init; } = null!;
}
