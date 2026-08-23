namespace Crm.Application.RiskEngine.Dtos;

public record CreateRiskMatrixDto(
    string Name,
    decimal AutoApproveThreshold,
    decimal AutoRejectThreshold,
    List<Guid> RuleIds);
