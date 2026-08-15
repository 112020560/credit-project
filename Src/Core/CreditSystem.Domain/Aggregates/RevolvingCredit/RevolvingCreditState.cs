using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.RevolvingCredit;

public record RevolvingCreditState
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public RevolvingCreditStatus Status { get; init; }
    public Money CreditLimit { get; init; } = null!;
    public Money CurrentBalance { get; init; } = null!;
    public Money AvailableCredit { get; init; } = null!;
    public Money AccruedInterest { get; init; } = null!;
    public Money PendingFees { get; init; } = null!;
    public InterestRate InterestRate { get; init; } = null!;
    public decimal MinimumPaymentPercentage { get; init; }
    public Money MinimumPaymentAmount { get; init; } = null!;
    public int BillingCycleDay { get; init; }
    public int GracePeriodDays { get; init; }
    public DateTime? ActivatedAt { get; init; }
    public DateTime? LastStatementDate { get; init; }
    public DateTime? NextStatementDate { get; init; }
    public DateTime? PaymentDueDate { get; init; }
    public Money? CurrentMinimumPayment { get; init; }
    public int ConsecutiveMissedPayments { get; init; }
    public DateTime? LastInterestAccrualDate { get; init; }
    public DateTime? FrozenAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public int Version { get; init; }

    public Money TotalOwed => CurrentBalance + AccruedInterest + PendingFees;

    public static RevolvingCreditState Initial => new()
    {
        Status = RevolvingCreditStatus.Pending,
        CreditLimit = Money.Zero(),
        CurrentBalance = Money.Zero(),
        AvailableCredit = Money.Zero(),
        AccruedInterest = Money.Zero(),
        PendingFees = Money.Zero(),
        MinimumPaymentAmount = Money.Zero(),
        ConsecutiveMissedPayments = 0,
        Version = 0
    };
}