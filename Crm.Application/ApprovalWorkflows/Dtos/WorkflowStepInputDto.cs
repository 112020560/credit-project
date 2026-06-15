namespace Crm.Application.ApprovalWorkflows.Dtos;

public record WorkflowStepInputDto(string StepName, int Order, string? RequiredRole);
