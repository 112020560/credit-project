namespace Crm.Domain.RiskEngine;

public interface IRiskEngine
{
    RiskEvaluation Evaluate(RiskMatrix matrix, CreditApplicationData data);
}
