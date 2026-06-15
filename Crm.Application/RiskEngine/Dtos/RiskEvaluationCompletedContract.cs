namespace Crm.Application.RiskEngine.Dtos;

public class RiskEvaluationCompletedContract
{
    public Guid RiskEvaluationId { get; set; }
    public Guid CreditApplicationId { get; set; }
    public Guid RiskMatrixId { get; set; }
    public int RiskMatrixVersion { get; set; }
    public decimal TotalScore { get; set; }
    public string Outcome { get; set; } = null!;
    public DateTime CompletedAt { get; set; }
}
