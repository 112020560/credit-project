using CreditSystem.Domain.Models.ReadModels;

namespace CreditSystem.Application.Queries.RevolvingCredit.GetRevolvingCreditSummary;

public record RevolvingCreditSummaryResponse
{
    public Guid CreditLineId { get; init; }
    public Guid CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string Status { get; init; } = null!;
    public decimal CreditLimit { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal AvailableCredit { get; init; }
    public decimal AccruedInterest { get; init; }
    public decimal PendingFees { get; init; }
    public decimal TotalOwed { get; init; }
    public decimal UtilizationRate { get; init; }
    public decimal InterestRate { get; init; }
    public decimal MinimumPaymentPercentage { get; init; }
    public decimal MinimumPaymentAmount { get; init; }
    public int BillingCycleDay { get; init; }
    public int GracePeriodDays { get; init; }
    public DateTime? ActivatedAt { get; init; }
    public DateTime? NextStatementDate { get; init; }
    public DateTime? PaymentDueDate { get; init; }
    public decimal? CurrentMinimumPayment { get; init; }
    public string Currency { get; init; } = "USD";

    public static RevolvingCreditSummaryResponse FromReadModel(RevolvingCreditSummaryReadModel model) => new()
    {
        CreditLineId = model.CreditLineId,
        CustomerId = model.CustomerId,
        CustomerName = model.CustomerName,
        Status = model.Status,
        CreditLimit = model.CreditLimit,
        CurrentBalance = model.CurrentBalance,
        AvailableCredit = model.AvailableCredit,
        AccruedInterest = model.AccruedInterest,
        PendingFees = model.PendingFees,
        TotalOwed = model.TotalOwed,
        UtilizationRate = model.UtilizationRate,
        InterestRate = model.InterestRate,
        MinimumPaymentPercentage = model.MinimumPaymentPercentage,
        MinimumPaymentAmount = model.MinimumPaymentAmount,
        BillingCycleDay = model.BillingCycleDay,
        GracePeriodDays = model.GracePeriodDays,
        ActivatedAt = model.ActivatedAt,
        NextStatementDate = model.NextStatementDate,
        PaymentDueDate = model.PaymentDueDate,
        CurrentMinimumPayment = model.CurrentMinimumPayment,
        Currency = model.Currency
    };
}