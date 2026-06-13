using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.LoanContract;

public record LoanContractState
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public ContractStatus Status { get; init; }
    public Money Principal { get; init; }
    public Money CurrentBalance { get; init; }
    public Money AccruedInterest { get; init; }
    public Money TotalFees { get; init; }
    public InterestRate InterestRate { get; init; }
    public int TermMonths { get; init; }
    public PaymentSchedule Schedule { get; init; }
    public int PaymentsMade { get; init; }
    public int PaymentsMissed { get; init; }
    public DateTime? LastPaymentDate { get; init; }
    public DateTime? NextPaymentDue { get; init; }
    public DateTime? DisbursedAt { get; init; }
    public DateTime? DefaultedAt { get; init; }
    public DateTime? PaidOffAt { get; init; }
    public int Version { get; init; }
    public DateTime? LastInterestAccrualDate { get; init; }
    public AmortizationMethod AmortizationMethod { get; init; }

    public Money TotalOwed => CurrentBalance + AccruedInterest + TotalFees;
    public bool IsDelinquent => PaymentsMissed > 0;
    public bool IsActive => Status == ContractStatus.Active;

    public static LoanContractState Initial => new()
    {
        Status = ContractStatus.Draft,
        CurrentBalance = Money.Zero(),
        AccruedInterest = Money.Zero(),
        TotalFees = Money.Zero(),
        PaymentsMade = 0,
        PaymentsMissed = 0,
        Version = 0
    };
}