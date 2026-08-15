namespace CreditSystem.Domain.Rules.Implementations;

public class ActiveLoansRule : IContractRule
{
    public string RuleName => "ActiveLoansCheck";
    public int Priority => 4;

    public Task<RuleEvaluationResult> EvaluateAsync(
        ContractEvaluationContext context, 
        CancellationToken ct = default)
    {
        if (!context.HasActiveLoans.HasValue)
        {
            return Task.FromResult(RuleEvaluationResult.Pass(
                RuleName,
                "Active loans status not available, skipping check",
                new Dictionary<string, object> { ["Skipped"] = true }
            ));
        }

        if (context.HasActiveLoans.Value)
        {
            return Task.FromResult(RuleEvaluationResult.Pass(
                RuleName,
                "Customer has active loans, applying rate adjustment",
                new Dictionary<string, object>
                {
                    ["HasActiveLoans"] = true,
                    ["RateAdjustment"] = 1.0m
                }
            ));
        }

        return Task.FromResult(RuleEvaluationResult.Pass(
            RuleName,
            "Customer has no active loans",
            new Dictionary<string, object>
            {
                ["HasActiveLoans"] = false,
                ["RateAdjustment"] = 0m
            }
        ));
    }
}