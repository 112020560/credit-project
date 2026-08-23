namespace Crm.Application.RiskEngine.Dtos;

public record RiskMatrixDto(
    Guid Id,
    string Name,
    int Version,
    string Status,
    decimal AutoApproveThreshold,
    decimal AutoRejectThreshold,
    DateTime CreatedAt);
