namespace CreditSystem.Domain.Rules.Implementations;

public class MaxLoanAmountRule : IContractRule, IHardStopRule
{
    public string RuleName => "MaxLoanAmountValidation";
    public int Priority => 0; // Primera regla en ejecutarse

    private const decimal AbsoluteMaxLoan = 500_000m;
    private const decimal IncomeMultiplier = 5m; // Máximo 5x ingreso anual

    public Task<RuleEvaluationResult> EvaluateAsync(
        ContractEvaluationContext context, 
        CancellationToken ct = default)
    {
        // Validar límite absoluto
        if (context.RequestedAmount > AbsoluteMaxLoan)
        {
            return Task.FromResult(RuleEvaluationResult.Fail(
                RuleName,
                $"Requested amount {context.RequestedAmount:C} exceeds absolute maximum of {AbsoluteMaxLoan:C}"
            ));
        }

        // Validar contra ingresos si están disponibles
        if (context.MonthlyIncome.HasValue && context.MonthlyIncome.Value > 0)
        {
            var annualIncome = context.MonthlyIncome.Value * 12;
            var maxBasedOnIncome = annualIncome * IncomeMultiplier;

            if (context.RequestedAmount > maxBasedOnIncome)
            {
                return Task.FromResult(RuleEvaluationResult.Fail(
                    RuleName,
                    $"Requested amount {context.RequestedAmount:C} exceeds {IncomeMultiplier}x annual income limit of {maxBasedOnIncome:C}"
                ));
            }
        }

        return Task.FromResult(RuleEvaluationResult.Pass(
            RuleName,
            $"Requested amount {context.RequestedAmount:C} is within acceptable limits"
        ));
    }
}