using CreditSystem.Domain.Models.ReadModels;

namespace CreditSystem.Application.Queries.GetDelinquentLoans;

public record DelinquentLoanResponse
{
    public Guid LoanId { get; init; }
    public Guid CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? CustomerEmail { get; init; }
    public decimal Principal { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal TotalOwed { get; init; }
    public int DaysOverdue { get; init; }
    public int PaymentsMissed { get; init; }
    public DateTime? LastPaymentAt { get; init; }
    public DateTime? NextActionDate { get; init; }
    public string CollectionStatus { get; init; } = null!;
    public string? AssignedCollector { get; init; }

    public static DelinquentLoanResponse FromReadModel(DelinquentLoanReadModel model) => new()
    {
        LoanId = model.LoanId,
        CustomerId = model.CustomerId,
        CustomerName = model.CustomerName,
        CustomerPhone = model.CustomerPhone,
        CustomerEmail = model.CustomerEmail,
        Principal = model.Principal,
        CurrentBalance = model.CurrentBalance,
        TotalOwed = model.TotalOwed,
        DaysOverdue = model.DaysOverdue,
        PaymentsMissed = model.PaymentsMissed,
        LastPaymentAt = model.LastPaymentAt,
        NextActionDate = model.NextActionDate,
        CollectionStatus = model.CollectionStatus,
        AssignedCollector = model.AssignedCollector
    };
}