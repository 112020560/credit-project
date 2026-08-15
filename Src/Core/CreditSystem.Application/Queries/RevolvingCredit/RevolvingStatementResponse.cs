namespace CreditSystem.Application.Queries.RevolvingCredit;

public record RevolvingStatementResponse
{
    public Guid StatementId { get; init; }
    public DateTime StatementDate { get; init; }
    public DateTime DueDate { get; init; }
    public decimal PreviousBalance { get; init; }
    public decimal Purchases { get; init; }
    public decimal Payments { get; init; }
    public decimal InterestCharged { get; init; }
    public decimal FeesCharged { get; init; }
    public decimal NewBalance { get; init; }
    public decimal MinimumPayment { get; init; }
    public bool IsPaid { get; init; }
    public DateTime? PaidAt { get; init; }
}