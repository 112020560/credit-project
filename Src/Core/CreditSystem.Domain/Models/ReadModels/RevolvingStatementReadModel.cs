namespace CreditSystem.Domain.Models.ReadModels;

public class RevolvingStatementReadModel
{
    public Guid StatementId { get; set; }
    public Guid CreditLineId { get; set; }
    public DateTime StatementDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal Purchases { get; set; }
    public decimal Payments { get; set; }
    public decimal InterestCharged { get; set; }
    public decimal FeesCharged { get; set; }
    public decimal NewBalance { get; set; }
    public decimal MinimumPayment { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}