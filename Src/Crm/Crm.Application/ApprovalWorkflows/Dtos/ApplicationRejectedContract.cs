namespace Crm.Application.ApprovalWorkflows.Dtos;

public record ApplicationRejectedContract(
    Guid CreditApplicationId,
    Guid ProspectId,
    Guid? WorkflowDefinitionId,
    string? RejectionReason,
    DateTime RejectedAt);
