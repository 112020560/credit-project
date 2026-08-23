namespace Crm.Application.ApprovalWorkflows.Dtos;

public record WorkflowStepDto(Guid Id, string StepName, int Order, string? RequiredRole);
