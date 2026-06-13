namespace CreditSystem.Domain.Rules.Implementations;

public class CollateralRule : IContractRule
{
    public string RuleName => "CollateralEvaluation";
    public int Priority => 3;

    private const decimal MinCollateralRatio = 1.2m; // Colateral debe ser 120% del préstamo

    public Task<RuleEvaluationResult> EvaluateAsync(
        ContractEvaluationContext context, 
        CancellationToken ct = default)
    {
        // Si no hay colateral, no es requerido pero no da beneficio
        if (!context.CollateralValue.HasValue || context.CollateralValue.Value <= 0)
        {
            return Task.FromResult(RuleEvaluationResult.Pass(
                RuleName,
                "No collateral provided, loan is unsecured",
                new Dictionary<string, object>
                {
                    ["Secured"] = false,
                    ["RateAdjustment"] = 1.0m // Penalización por no tener colateral
                }
            ));
        }

        var collateralRatio = context.CollateralValue.Value / context.RequestedAmount;

        if (collateralRatio < 1.0m)
        {
            return Task.FromResult(RuleEvaluationResult.Pass(
                RuleName,
                $"Collateral ratio {collateralRatio:P2} is below loan amount, partial security",
                new Dictionary<string, object>
                {
                    ["CollateralRatio"] = collateralRatio,
                    ["Secured"] = false,
                    ["RateAdjustment"] = 0.5m
                }
            ));
        }

        // Beneficio por colateral suficiente
        var rateDiscount = collateralRatio >= MinCollateralRatio ? -1.5m : -0.5m;

        return Task.FromResult(RuleEvaluationResult.Pass(
            RuleName,
            $"Collateral ratio {collateralRatio:P2} provides security for the loan",
            new Dictionary<string, object>
            {
                ["CollateralRatio"] = collateralRatio,
                ["Secured"] = true,
                ["RateAdjustment"] = rateDiscount
            }
        ));
    }
}