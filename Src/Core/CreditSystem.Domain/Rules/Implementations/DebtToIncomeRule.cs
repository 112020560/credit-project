namespace CreditSystem.Domain.Rules.Implementations;

public class DebtToIncomeRule : IContractRule, IHardStopRule
{
    public string RuleName => "DebtToIncomeRatio";
    public int Priority => 2;

    public Task<RuleEvaluationResult> EvaluateAsync(
        ContractEvaluationContext context,
        CancellationToken ct = default)
    {
        if (context.MonthlyIncome == null || context.MonthlyIncome.Amount <= 0)
        {
            return Task.FromResult(RuleEvaluationResult.Pass(
                RuleName,
                "Income data not available, skipping DTI evaluation",
                new Dictionary<string, object> { ["Skipped"] = true }
            ));
        }

        var policy = context.Policy;
        var effectiveRate = context.Product?.Rates.BaseInterestRate ?? policy.BaseInterestRate;
        var monthlyDebt = context.MonthlyDebt?.Amount ?? 0;
        var estimatedPayment = CalculateMonthlyPayment(context.RequestedAmount.Amount, context.TermMonths, effectiveRate);
        var totalDebt = monthlyDebt + estimatedPayment;
        var dtiRatio = totalDebt / context.MonthlyIncome.Amount;

        if (dtiRatio > policy.MaxDtiRatio)
        {
            return Task.FromResult(RuleEvaluationResult.Fail(
                RuleName,
                $"DTI ratio {dtiRatio:P2} exceeds maximum allowed {policy.MaxDtiRatio:P0}",
                new Dictionary<string, object>
                {
                    ["DTI"] = dtiRatio,
                    ["MaxDtiRatio"] = policy.MaxDtiRatio,
                    ["MonthlyIncome"] = context.MonthlyIncome.Amount,
                    ["TotalMonthlyDebt"] = totalDebt
                }
            ));
        }

        var warningThreshold = policy.MaxDtiRatio * 0.80m;
        var rateAdjustment = dtiRatio > warningThreshold ? 2.0m : 0.0m;

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

    private static decimal CalculateMonthlyPayment(decimal amount, int months, decimal annualRatePercent)
    {
        var monthlyRate = annualRatePercent / 12m / 100m;
        if (monthlyRate == 0) return amount / months;

        var payment = amount * (monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), months))
                      / ((decimal)Math.Pow((double)(1 + monthlyRate), months) - 1);
        return payment;
    }
}
