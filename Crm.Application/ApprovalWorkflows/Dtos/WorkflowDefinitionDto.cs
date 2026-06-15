namespace Crm.Application.ApprovalWorkflows.Dtos;

public record WorkflowDefinitionDto(Guid Id, string Name, string Status, IReadOnlyList<WorkflowStepDto> Steps, DateTime CreatedAt);
