namespace Crm.Application.ApprovalWorkflows.Dtos;

public record ApprovalRequestedContract(
    Guid CreditApplicationId,
    Guid? WorkflowDefinitionId,
    Guid? WorkflowStepId,
    string? StepName,
    int StepOrder,
    DateTime RequestedAt);
