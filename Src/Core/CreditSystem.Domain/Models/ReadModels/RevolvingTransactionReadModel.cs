namespace CreditSystem.Domain.Models.ReadModels;

public class RevolvingTransactionReadModel
{
    public Guid Id { get; set; }
    public Guid CreditLineId { get; set; }
    public string TransactionType { get; set; } = null!;  // Draw, Payment, Interest, Fee
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime CreatedAt { get; set; }
}