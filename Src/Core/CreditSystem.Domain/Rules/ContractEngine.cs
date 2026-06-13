using Microsoft.Extensions.Logging;

namespace CreditSystem.Domain.Rules;

public class ContractEngine
{
    private readonly IEnumerable<IContractRule> _rules;
    private readonly ILogger<ContractEngine> _logger;
    private const decimal BaseInterestRate = 8.0m; // Tasa base

    public ContractEngine(IEnumerable<IContractRule> rules, ILogger<ContractEngine> logger)
    {
        _rules = rules.OrderBy(r => r.Priority);
        _logger = logger;
    }

    public async Task<ContractEvaluationResponse> EvaluateAsync(
        ContractEvaluationContext context, 
        CancellationToken ct = default)
    {
        var results = new List<RuleEvaluationResult>();
        var approved = true;
        var rateAdjustment = 0m;

        foreach (var rule in _rules)
        {
            try
            {
                var result = await rule.EvaluateAsync(context, ct);
                results.Add(result);

                _logger.LogInformation(
                    "Rule {Rule} evaluated: Passed={Passed}, Message={Message}",
                    rule.RuleName, result.Passed, result.Message);

                if (!result.Passed)
                {
                    approved = false;

                    // Si es una regla crítica, detener evaluación
                    if (rule is IHardStopRule)
                    {
                        _logger.LogWarning(
                            "Hard stop rule {Rule} failed, stopping evaluation",
                            rule.RuleName);
                        break;
                    }
                }

                // Acumular ajuste de tasa
                if (result.Metadata?.TryGetValue("RateAdjustment", out var adj) == true)
                {
                    rateAdjustment += Convert.ToDecimal(adj);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating rule {Rule}", rule.RuleName);
                
                results.Add(RuleEvaluationResult.Fail(
                    rule.RuleName, 
                    $"Error evaluating rule: {ex.Message}"));
                
                approved = false;
            }
        }

        var finalRate = BaseInterestRate + rateAdjustment;

        return approved
            ? ContractEvaluationResponse.Approve(finalRate, results)
            : ContractEvaluationResponse.Reject(results);
    }
}