namespace CreditSystem.Domain.Rules.Implementations;

public class ProductEligibilityRule : IContractRule, IHardStopRule
{
    public string RuleName => "ProductEligibility";
    public int Priority => 1;

    public Task<RuleEvaluationResult> EvaluateAsync(
        ContractEvaluationContext context,
        CancellationToken ct = default)
    {
        if (context.Product == null)
        {
            return Task.FromResult(RuleEvaluationResult.Pass(
                RuleName,
                "No product specified — rule skipped",
                new Dictionary<string, object> { ["Skipped"] = true }));
        }

        var product = context.Product;
        var limits = product.Limits;
        var amount = context.RequestedAmount.Amount;
        var term = context.TermMonths;

        // Validate amount range
        if (amount < limits.MinAmount || amount > limits.MaxAmount)
        {
            return Task.FromResult(RuleEvaluationResult.Fail(
                RuleName,
                $"Requested amount {amount:N2} is outside the allowed range for product '{product.Name}' " +
                $"[{limits.MinAmount:N2} – {limits.MaxAmount:N2}]"));
        }

        // Validate term range
        if (term < limits.MinTermMonths || term > limits.MaxTermMonths)
        {
            return Task.FromResult(RuleEvaluationResult.Fail(
                RuleName,
                $"Requested term {term} months is outside the allowed range for product '{product.Name}' " +
                $"[{limits.MinTermMonths} – {limits.MaxTermMonths} months]"));
        }

        // Validate collateral requirement
        if (product.RequiresCollateral)
        {
            var collateral = context.CollateralValue?.Amount ?? 0m;

            if (collateral <= 0)
            {
                return Task.FromResult(RuleEvaluationResult.Fail(
                    RuleName,
                    $"Product '{product.Name}' requires collateral but none was provided"));
            }

            // Validate LTV if defined
            if (product.Rates.MaxLtv.HasValue && amount > 0)
            {
                var ltv = amount / collateral;
                if (ltv > product.Rates.MaxLtv.Value)
                {
                    return Task.FromResult(RuleEvaluationResult.Fail(
                        RuleName,
                        $"LTV {ltv:P2} exceeds maximum allowed {product.Rates.MaxLtv.Value:P2} for product '{product.Name}'"));
                }
            }
        }

        return Task.FromResult(RuleEvaluationResult.Pass(
            RuleName,
            $"Contract meets eligibility requirements for product '{product.Name}'",
            new Dictionary<string, object>
            {
                ["ProductId"] = product.Id,
                ["ProductName"] = product.Name
            }));
    }
}
