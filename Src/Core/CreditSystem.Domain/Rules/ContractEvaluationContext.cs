using CreditSystem.Domain.Entities;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Rules;

public record ContractEvaluationContext
{
    public CustomerCreditProfile Customer { get; init; } = null!;
    public Money RequestedAmount { get; init; } = null!;
    public int TermMonths { get; init; }
    public Money? CollateralValue { get; init; }
    public int? CreditScore { get; init; }
    public Money? MonthlyIncome { get; init; }
    public Money? MonthlyDebt { get; init; }
    public bool? HasActiveLoans { get; init; }
    public Money? MemberSharesAmount { get; init; }
    public bool? IsActiveMember { get; init; }
}
