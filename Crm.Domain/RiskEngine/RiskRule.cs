namespace Crm.Domain.RiskEngine;

public class RiskRule
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string RuleType { get; set; } = null!;
    public string TargetField { get; set; } = null!;
    public Dictionary<string, string> Parameters { get; set; } = new();
    public decimal Weight { get; set; }
    public DateTime CreatedAt { get; set; }
}
