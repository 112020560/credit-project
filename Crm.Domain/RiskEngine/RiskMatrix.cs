namespace Crm.Domain.RiskEngine;

public class RiskMatrix
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int Version { get; set; } = 1;
    public string Status { get; set; } = RiskMatrixStatus.Draft;
    public decimal AutoApproveThreshold { get; set; }
    public decimal AutoRejectThreshold { get; set; }
    public List<PricingBand>? PricingBands { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual ICollection<RiskMatrixRule> MatrixRules { get; set; } = new List<RiskMatrixRule>();
}

public class PricingBand
{
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal InterestRate { get; set; }
    public decimal MaxAmount { get; set; }
}
