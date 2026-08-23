namespace Crm.Application.ApprovalWorkflows.Dtos;

public record ApprovalDecisionDto(Guid Id, Guid? WorkflowStepId, string Decision, string? RejectionReason, string? DecidedBy, DateTime DecidedAt);
