namespace CreditSystem.Domain.Rules.Implementations;

public class DebtToIncomeRule : IContractRule
{
    public string RuleName => "DebtToIncomeRatio";
    public int Priority => 2;

    private const decimal MaxDtiRatio = 0.50m; // 50%
    private const decimal WarningDtiRatio = 0.40m; // 40%

    public Task<RuleEvaluationResult> EvaluateAsync(
        ContractEvaluationContext context, 
        CancellationToken ct = default)
    {
        // Si no hay datos de ingresos, pasar la regla
        if (!context.MonthlyIncome.HasValue || context.MonthlyIncome.Value <= 0)
        {
            return Task.FromResult(RuleEvaluationResult.Pass(
                RuleName,
                "Income data not available, skipping DTI evaluation",
                new Dictionary<string, object> { ["Skipped"] = true }
            ));
        }

        var monthlyDebt = context.MonthlyDebt ?? 0;
        var estimatedPayment = CalculateMonthlyPayment(context.RequestedAmount, context.TermMonths);
        var totalDebt = monthlyDebt + estimatedPayment;
        var dtiRatio = totalDebt / context.MonthlyIncome.Value;

        if (dtiRatio > MaxDtiRatio)
        {
            return Task.FromResult(RuleEvaluationResult.Fail(
                RuleName,
                $"DTI ratio {dtiRatio:P2} exceeds maximum allowed {MaxDtiRatio:P0}",
                new Dictionary<string, object>
                {
                    ["DTI"] = dtiRatio,
                    ["MonthlyIncome"] = context.MonthlyIncome.Value,
                    ["TotalMonthlyDebt"] = totalDebt
                }
            ));
        }

        var rateAdjustment = dtiRatio > WarningDtiRatio ? 2.0m : 0.0m;

        return Task.FromResult(RuleEvaluationResult.Pass(
            RuleName,
            $"DTI ratio {dtiRatio:P2} is within acceptable limits",
            new Dictionary<string, object>
            {
                ["DTI"] = dtiRatio,
                ["RateAdjustment"] = rateAdjustment
            }
        ));
    }

    private static decimal CalculateMonthlyPayment(decimal amount, int months)
    {
        // Estimación simple para evaluación
        var estimatedRate = 0.12m / 12; // 12% anual
        if (estimatedRate == 0) return amount / months;
        
        var payment = amount * (estimatedRate * (decimal)Math.Pow((double)(1 + estimatedRate), months)) 
                      / ((decimal)Math.Pow((double)(1 + estimatedRate), months) - 1);
        return payment;
    }
}
