namespace CreditSystem.Domain.Rules.Implementations;

public class CreditScoreRule : IContractRule, IHardStopRule
{
    public string RuleName => "CreditScoreEvaluation";
    public int Priority => 1;

    private const int MinimumScore = 500;

    public Task<RuleEvaluationResult> EvaluateAsync(
        ContractEvaluationContext context, 
        CancellationToken ct = default)
    {
        var score = context.CreditScore;

        // Si no hay score, pasar la regla (se evaluará con otros criterios)
        if (!score.HasValue)
        {
            return Task.FromResult(RuleEvaluationResult.Pass(
                RuleName,
                "Credit score not available, skipping rule",
                new Dictionary<string, object> { ["Skipped"] = true }
            ));
        }

        if (score.Value < MinimumScore)
        {
            return Task.FromResult(RuleEvaluationResult.Fail(
                RuleName,
                $"Credit score {score.Value} is below minimum threshold of {MinimumScore}"
            ));
        }

        // Calcular ajuste de tasa basado en score
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