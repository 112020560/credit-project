namespace CreditSystem.Domain.Models.ReadModels;

public class DefaultedLoanReadModel
{
    public Guid LoanId { get; init; }
    public Guid CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public decimal Principal { get; init; }
    public decimal OutstandingBalance { get; init; }
    public decimal AccruedInterest { get; init; }
    public decimal TotalFees { get; init; }
    public decimal TotalOwed { get; init; }
    public int PaymentsMissed { get; init; }
    public DateTime? LastPaymentAt { get; init; }
    public DateTime DefaultedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}