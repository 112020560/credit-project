namespace Crm.Application.RiskEngine.Dtos;

public class RiskEvaluationStartedContract
{
    public Guid RiskEvaluationId { get; set; }
    public Guid CreditApplicationId { get; set; }
    public Guid RiskMatrixId { get; set; }
    public int RiskMatrixVersion { get; set; }
    public DateTime StartedAt { get; set; }
}
