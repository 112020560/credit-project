namespace CreditSystem.Domain.Rules;

public class ContractEngine
{
    private readonly IEnumerable<IContractRule> _rules;

    public ContractEngine(IEnumerable<IContractRule> rules)
    {
        _rules = rules.OrderBy(r => r.Priority);
    }

    public async Task<ContractEvaluationResponse> EvaluateAsync(
        ContractEvaluationContext context,
        decimal? baseInterestRate = null,
        CancellationToken ct = default)
    {
        var effectiveBaseRate = baseInterestRate ?? context.Policy.BaseInterestRate;
        var results = new List<RuleEvaluationResult>();
        var approved = true;
        var rateAdjustment = 0m;

        foreach (var rule in _rules)
        {
            try
            {
                var result = await rule.EvaluateAsync(context, ct);
                results.Add(result);

                if (!result.Passed)
                {
                    approved = false;

                    if (rule is IHardStopRule)
                        break;
                }

                if (result.Metadata?.TryGetValue("RateAdjustment", out var adj) == true)
                    rateAdjustment += Convert.ToDecimal(adj);
            }
            catch (Exception ex)
            {
                results.Add(RuleEvaluationResult.Fail(
                    rule.RuleName,
                    $"Error evaluating rule: {ex.Message}"));

                approved = false;
            }
        }

        var finalRate = effectiveBaseRate + rateAdjustment;

        return approved
            ? ContractEvaluationResponse.Approve(finalRate, results)
            : ContractEvaluationResponse.Reject(results);
    }
}