using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Models;

namespace CreditSystem.Domain.Rules.Implementations;

public class CreditScoreRule : IContractRule, IHardStopRule
{
    public string RuleName => "CreditScoreEvaluation";
    public int Priority => 1;

    private const int MinimumScore = 500;
    private readonly UnderwritingPolicy _policy;

    public CreditScoreRule(UnderwritingPolicy policy)
    {
        _policy = policy;
    }

    public Task<RuleEvaluationResult> EvaluateAsync(
        ContractEvaluationContext context,
        CancellationToken ct = default)
    {
        var score = context.CreditScore;

        if (!score.HasValue)
        {
            return _policy.NoScoreBehavior == NoScoreBehavior.Reject
                ? Task.FromResult(RuleEvaluationResult.Fail(
                    RuleName,
                    "Credit score required — policy rejects applications without score"))
                : Task.FromResult(RuleEvaluationResult.Pass(
                    RuleName,
                    "Credit score not available, approved with penalty rate",
                    new Dictionary<string, object>
                    {
                        ["Skipped"] = true,
                        ["RateAdjustment"] = 5.0m
                    }));
        }

        if (score.Value < MinimumScore)
        {
            return Task.FromResult(RuleEvaluationResult.Fail(
                RuleName,
                $"Credit score {score.Value} is below minimum threshold of {MinimumScore}"
            ));
        }

        var rateAdjustment = score.Value switch
        {
            >= 750 => 0.0m,    // Excelente - tasa base
            >= 700 => 1.5m,    // Muy bueno
            >= 650 => 3.0m,    // Bueno
            >= 600 => 5.0m,    // Regular
            >= 550 => 8.0m,    // Bajo
            _ => 12.0m         // Muy bajo
        };

        return Task.FromResult(RuleEvaluationResult.Pass(
            RuleName,
            $"Credit score {score.Value} approved with rate adjustment of {rateAdjustment}%",
            new Dictionary<string, object>
            {
                ["CreditScore"] = score.Value,
                ["RateAdjustment"] = rateAdjustment
            }
        ));
    }
}