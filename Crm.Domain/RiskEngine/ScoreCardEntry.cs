namespace Crm.Domain.RiskEngine;

public class ScoreCardEntry
{
    public Guid Id { get; set; }
    public Guid RiskEvaluationId { get; set; }
    public Guid RuleId { get; set; }
    public string RuleName { get; set; } = null!;
    public string TargetField { get; set; } = null!;
    public string? ObservedValue { get; set; }
    public bool Passed { get; set; }
    public decimal WeightedContribution { get; set; }

    public virtual RiskEvaluation RiskEvaluation { get; set; } = null!;
}
