namespace CreditSystem.Application.Queries.GetPayoffAmount;

public record PayoffAmountResponse
{
    public Guid LoanId { get; init; }
    public decimal PrincipalBalance { get; init; }
    public decimal AccruedInterest { get; init; }
    public decimal PendingFees { get; init; }
    public decimal TotalPayoffAmount { get; init; }
    public DateTime CalculatedAsOf { get; init; }
    public DateTime ValidUntil { get; init; }
    public string Currency { get; init; } = "USD";
}