namespace CreditSystem.Domain.Models.ReadModels;

public class PaymentHistoryReadModel
{
    public Guid Id { get; set; }
    public Guid LoanId { get; set; }
    public int PaymentNumber { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PrincipalPaid { get; set; }
    public decimal InterestPaid { get; set; }
    public decimal FeesPaid { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? PaymentMethod { get; set; }
    public string Status { get; set; } = "completed";
}