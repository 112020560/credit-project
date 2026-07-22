namespace CreditSystem.Domain.Rules.Implementations;

public class MaxLoanAmountRule : IContractRule, IHardStopRule
{
    public string RuleName => "MaxLoanAmountValidation";
    public int Priority => 0; // Primera regla en ejecutarse

    private const decimal AbsoluteMaxLoan = 500_000m;
    private const decimal IncomeMultiplier = 5m; // Max 5x annual income

    public Task<RuleEvaluationResult> EvaluateAsync(
        ContractEvaluationContext context, 
        CancellationToken ct = default)
    {
        if (context.RequestedAmount.Amount > AbsoluteMaxLoan)
        {
            return Task.FromResult(RuleEvaluationResult.Fail(
                RuleName,
                $"Requested amount {context.RequestedAmount.Amount:C} exceeds absolute maximum of {AbsoluteMaxLoan:C}"
            ));
        }

        // Enforce income-based cap if income data is available
        if (context.MonthlyIncome != null && context.MonthlyIncome.Amount > 0)
        {
            var annualIncome = context.MonthlyIncome.Amount * 12;
            var maxBasedOnIncome = annualIncome * IncomeMultiplier;

            if (context.RequestedAmount.Amount > maxBasedOnIncome)
            {
                return Task.FromResult(RuleEvaluationResult.Fail(
                    RuleName,
                    $"Requested amount {context.RequestedAmount.Amount:C} exceeds {IncomeMultiplier}x annual income limit of {maxBasedOnIncome:C}"
                ));
            }
        }

        return Task.FromResult(RuleEvaluationResult.Pass(
            RuleName,
            $"Requested amount {context.RequestedAmount.Amount:C} is within acceptable limits"
        ));
    }
}