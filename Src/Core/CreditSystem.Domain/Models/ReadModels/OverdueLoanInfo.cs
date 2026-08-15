namespace CreditSystem.Domain.Models.ReadModels;

public record OverdueLoanInfo
{
    public Guid LoanId { get; init; }
    public Guid CustomerId { get; init; }
    public int PaymentNumber { get; init; }
    public DateTime DueDate { get; init; }
    public decimal AmountDue { get; init; }
    public string Currency { get; init; } = "USD";
    public int DaysOverdue { get; init; }
    public bool AlreadyRecorded { get; init; }
}