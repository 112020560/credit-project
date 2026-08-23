namespace Crm.Application.ApprovalWorkflows.Dtos;

public record ApplicationApprovedContract(
    Guid CreditApplicationId,
    Guid ProspectId,
    Guid? WorkflowDefinitionId,
    DateTime ApprovedAt);
