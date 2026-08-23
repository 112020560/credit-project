namespace Crm.Domain.RiskEngine;

public class RiskEvaluation
{
    public Guid Id { get; set; }
    public Guid CreditApplicationId { get; set; }
    public Guid RiskMatrixId { get; set; }
    public int RiskMatrixVersion { get; set; }
    public decimal TotalScore { get; set; }
    public string Outcome { get; set; } = null!;
    public decimal? SuggestedInterestRate { get; set; }
    public decimal? SuggestedMaxAmount { get; set; }
    public DateTime EvaluatedAt { get; set; }

    public virtual ICollection<ScoreCardEntry> Entries { get; set; } = new List<ScoreCardEntry>();
}
