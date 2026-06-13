using CreditSystem.Domain.Entities;

namespace CreditSystem.Domain.Rules;

public record ContractEvaluationContext
{
    public CustomerReference Customer { get; init; } = null!;
    public decimal RequestedAmount { get; init; }
    public int TermMonths { get; init; }
    public decimal? CollateralValue { get; init; }
    
    // Datos adicionales que pueden venir del CRM o calcularse
    public int? CreditScore { get; init; }
    public decimal? MonthlyIncome { get; init; }
    public decimal? MonthlyDebt { get; init; }
    public bool? HasActiveLoans { get; init; }
}
