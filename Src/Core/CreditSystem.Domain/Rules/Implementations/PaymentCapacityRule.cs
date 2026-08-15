namespace CreditSystem.Domain.Rules.Implementations;

public class PaymentCapacityRule : IContractRule, IHardStopRule
{
    public string RuleName => "PaymentCapacityEvaluation";
    public int Priority => 2;

    public Task<RuleEvaluationResult> EvaluateAsync(
        ContractEvaluationContext context,
        CancellationToken ct = default)
    {
        if (context.MonthlyIncome == null || context.MonthlyIncome.Amount <= 0)
        {
            return Task.FromResult(RuleEvaluationResult.Pass(
                RuleName,
                "Income data not available, skipping payment capacity evaluation",
                new Dictionary<string, object> { ["Skipped"] = true }
            ));
        }

        var policy = context.Policy;
        var monthlyDebt = context.MonthlyDebt?.Amount ?? 0m;
        var maxMonthlyPayment = context.MonthlyIncome.Amount * policy.MaxDtiRatio - monthlyDebt;

        if (maxMonthlyPayment <= 0)
        {
            return Task.FromResult(RuleEvaluationResult.Fail(
                RuleName,
                $"No payment capacity available: existing debt ({monthlyDebt:N2}) already reaches or exceeds " +
                $"{policy.MaxDtiRatio:P0} of monthly income ({context.MonthlyIncome.Amount:N2})"
            ));
        }

        var effectiveAnnualRate = context.Product?.Rates.BaseInterestRate ?? policy.BaseInterestRate;
        var maxFinanciable = CalculatePresentValue(maxMonthlyPayment, context.TermMonths, effectiveAnnualRate);
        var requested = context.RequestedAmount.Amount;

        if (requested > maxFinanciable)
        {
            return Task.FromResult(RuleEvaluationResult.Fail(
                RuleName,
                $"Requested amount {requested:N2} exceeds maximum financiable amount {maxFinanciable:N2} " +
                $"based on available monthly payment capacity of {maxMonthlyPayment:N2}",
                new Dictionary<string, object>
                {
                    ["MaxFinanciableAmount"] = maxFinanciable,
                    ["MaxMonthlyPayment"] = maxMonthlyPayment,
                    ["EffectiveAnnualRate"] = effectiveAnnualRate
                }
            ));
        }

        return Task.FromResult(RuleEvaluationResult.Pass(
            RuleName,
            $"Requested amount {requested:N2} is within payment capacity (max financiable: {maxFinanciable:N2})",
            new Dictionary<string, object>
            {
                ["MaxFinanciableAmount"] = maxFinanciable,
                ["MaxMonthlyPayment"] = maxMonthlyPayment,
                ["EffectiveAnnualRate"] = effectiveAnnualRate
            }
        ));
    }

    private static decimal CalculatePresentValue(decimal monthlyPayment, int months, decimal annualRatePercent)
    {
        var r = annualRatePercent / 12m / 100m;
        if (r == 0) return monthlyPayment * months;

        var pv = monthlyPayment * (1m - (decimal)Math.Pow((double)(1 + r), -months)) / r;
        return pv;
    }
}
