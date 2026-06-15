namespace Crm.Application.RiskEngine.Dtos;

public record CreateRiskRuleDto(
    string Name,
    string RuleType,
    string TargetField,
    Dictionary<string, string> Parameters,
    decimal Weight);
