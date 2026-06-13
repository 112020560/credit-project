namespace CreditSystem.Domain.Models.ReadModels;

public class UpcomingPaymentReadModel
{
    public Guid Id { get; set; }
    public Guid LoanId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public int PaymentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal AmountDue { get; set; }
    public decimal PrincipalDue { get; set; }
    public decimal InterestDue { get; set; }
    public bool IsOverdue { get; set; }
    public int? DaysUntilDue { get; set; }
    public bool ReminderSent { get; set; }
}