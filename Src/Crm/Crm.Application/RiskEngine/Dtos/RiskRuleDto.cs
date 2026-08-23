namespace Crm.Application.RiskEngine.Dtos;

public record RiskRuleDto(
    Guid Id,
    string Name,
    string RuleType,
    string TargetField,
    Dictionary<string, string> Parameters,
    decimal Weight,
    DateTime CreatedAt);
