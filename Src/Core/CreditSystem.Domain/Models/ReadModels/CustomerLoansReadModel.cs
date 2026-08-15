namespace CreditSystem.Domain.Models.ReadModels;

public class CustomerLoansReadModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid LoanId { get; set; }
    public decimal Principal { get; set; }
    public decimal CurrentBalance { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}