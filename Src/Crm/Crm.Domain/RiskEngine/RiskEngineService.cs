namespace Crm.Domain.RiskEngine;

public class RiskEngineService : IRiskEngine
{
    public RiskEvaluation Evaluate(RiskMatrix matrix, CreditApplicationData data)
    {
        var entries = new List<ScoreCardEntry>();
        decimal totalScore = 0;

        foreach (var matrixRule in matrix.MatrixRules.OrderBy(r => r.Order))
        {
            var rule = matrixRule.RiskRule;
            var observed = GetFieldValue(data, rule.TargetField);
            var passed = EvaluateRule(rule, observed);
            var contribution = passed ? rule.Weight : 0;
            totalScore += contribution;

            entries.Add(new ScoreCardEntry
            {
                Id = Guid.CreateVersion7(),
                RuleId = rule.Id,
                RuleName = rule.Name,
                TargetField = rule.TargetField,
                ObservedValue = observed?.ToString(),
                Passed = passed,
                WeightedContribution = contribution
            });
        }

        var outcome = DetermineOutcome(matrix, totalScore);
        var (interestRate, maxAmount) = GetPricing(matrix, totalScore);

        return new RiskEvaluation
        {
            Id = Guid.CreateVersion7(),
            CreditApplicationId = data.ApplicationId,
            RiskMatrixId = matrix.Id,
            RiskMatrixVersion = matrix.Version,
            TotalScore = totalScore,
            Outcome = outcome,
            SuggestedInterestRate = interestRate,
            SuggestedMaxAmount = maxAmount,
            EvaluatedAt = DateTime.UtcNow,
            Entries = entries
        };
    }

    private static string DetermineOutcome(RiskMatrix matrix, decimal score)
    {
        if (score >= matrix.AutoApproveThreshold) return RiskEvaluationOutcome.AutoApprove;
        if (score <= matrix.AutoRejectThreshold) return RiskEvaluationOutcome.AutoReject;
        return RiskEvaluationOutcome.ManualReview;
    }

    private static (decimal? interestRate, decimal? maxAmount) GetPricing(RiskMatrix matrix, decimal score)
    {
        if (matrix.PricingBands is null || matrix.PricingBands.Count == 0)
            return (null, null);

        var band = matrix.PricingBands
            .FirstOrDefault(b => score >= b.MinScore && score <= b.MaxScore);

        return band is null ? (null, null) : (band.InterestRate, band.MaxAmount);
    }

    private static object? GetFieldValue(CreditApplicationData data, string field) => field switch
    {
        "AgeYears" => data.AgeYears,
        "MonthlyIncome" => data.MonthlyIncome,
        "HasAddress" => data.HasAddress,
        "HasWorkInfo" => data.HasWorkInfo,
        "HasFiscalInfo" => data.HasFiscalInfo,
        "DocumentCount" => data.DocumentCount,
        _ => null
    };

    private static bool EvaluateRule(RiskRule rule, object? value)
    {
        return rule.RuleType switch
        {
            RuleType.RangeCheck => EvaluateRange(rule.Parameters, value),
            RuleType.ThresholdCheck => EvaluateThreshold(rule.Parameters, value),
            RuleType.EnumCheck => EvaluateEnum(rule.Parameters, value),
            _ => false
        };
    }

    private static bool EvaluateRange(Dictionary<string, string> p, object? value)
    {
        if (value is null) return false;
        if (!decimal.TryParse(value.ToString(), out var num)) return false;
        if (p.TryGetValue("Min", out var minStr) && decimal.TryParse(minStr, out var min) && num < min) return false;
        if (p.TryGetValue("Max", out var maxStr) && decimal.TryParse(maxStr, out var max) && num > max) return false;
        return true;
    }

    private static bool EvaluateThreshold(Dictionary<string, string> p, object? value)
    {
        if (value is null) return false;
        if (!decimal.TryParse(value.ToString(), out var num)) return false;
        if (!p.TryGetValue("Value", out var valStr) || !decimal.TryParse(valStr, out var threshold)) return false;
        var direction = p.TryGetValue("Direction", out var dir) ? dir : "above";
        return direction == "above" ? num >= threshold : num <= threshold;
    }

    private static bool EvaluateEnum(Dictionary<string, string> p, object? value)
    {
        if (value is null) return false;
        if (!p.TryGetValue("AllowedValues", out var allowed)) return false;
        var allowedSet = allowed.Split(',', StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return allowedSet.Contains(value.ToString() ?? string.Empty);
    }
}
