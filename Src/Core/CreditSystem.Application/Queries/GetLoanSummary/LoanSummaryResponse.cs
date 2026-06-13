using CreditSystem.Domain.Models.ReadModels;

namespace CreditSystem.Application.Queries.GetLoanSummary;

public record LoanSummaryResponse
{
    public Guid LoanId { get; init; }
    public Guid CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public decimal Principal { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal AccruedInterest { get; init; }
    public decimal TotalFees { get; init; }
    public decimal TotalOwed { get; init; }
    public decimal InterestRate { get; init; }
    public int TermMonths { get; init; }
    public string Status { get; init; } = null!;
    public int PaymentsMade { get; init; }
    public int PaymentsMissed { get; init; }
    public DateTime? NextPaymentDate { get; init; }
    public decimal? NextPaymentAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DisbursedAt { get; init; }
    public DateTime? PaidOffAt { get; init; }

    public static LoanSummaryResponse FromReadModel(LoanSummaryReadModel model) => new()
    {
        LoanId = model.LoanId,
        CustomerId = model.CustomerId,
        CustomerName = model.CustomerName,
        Principal = model.Principal,
        CurrentBalance = model.CurrentBalance,
        AccruedInterest = model.AccruedInterest,
        TotalFees = model.TotalFees,
        TotalOwed = model.TotalOwed,
        InterestRate = model.InterestRate,
        TermMonths = model.TermMonths,
        Status = model.Status,
        PaymentsMade = model.PaymentsMade,
        PaymentsMissed = model.PaymentsMissed,
        NextPaymentDate = model.NextPaymentDate,
        NextPaymentAmount = model.NextPaymentAmount,
        CreatedAt = model.CreatedAt,
        DisbursedAt = model.DisbursedAt,
        PaidOffAt = model.PaidOffAt
    };
}