namespace Crm.Application.RiskEngine.Dtos;

public record ScoreCardEntryDto(
    Guid RuleId,
    string RuleName,
    string TargetField,
    string? ObservedValue,
    bool Passed,
    decimal WeightedContribution);
