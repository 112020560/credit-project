namespace CreditSystem.Domain.Models.ReadModels;

public class DelinquentLoanReadModel
{
    public Guid LoanId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public decimal Principal { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal TotalOwed { get; set; }
    public int DaysOverdue { get; set; }
    public int PaymentsMissed { get; set; }
    public DateTime? LastPaymentAt { get; set; }
    public DateTime? NextActionDate { get; set; }
    public string CollectionStatus { get; set; } = "pending_contact";
    public string? AssignedCollector { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}