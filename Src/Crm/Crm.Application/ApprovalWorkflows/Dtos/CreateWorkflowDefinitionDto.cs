namespace Crm.Application.ApprovalWorkflows.Dtos;

public record CreateWorkflowDefinitionDto(string Name, List<WorkflowStepInputDto> Steps);
