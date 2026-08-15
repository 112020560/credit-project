namespace CreditSystem.Domain.Models.ReadModels;

public record ActiveLoanForAccrual
{
    public Guid LoanId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal InterestRate { get; init; }
}