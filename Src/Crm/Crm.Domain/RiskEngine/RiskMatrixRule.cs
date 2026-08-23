namespace Crm.Domain.RiskEngine;

public class RiskMatrixRule
{
    public Guid Id { get; set; }
    public Guid RiskMatrixId { get; set; }
    public Guid RiskRuleId { get; set; }
    public int Order { get; set; }

    public virtual RiskMatrix RiskMatrix { get; set; } = null!;
    public virtual RiskRule RiskRule { get; set; } = null!;
}
