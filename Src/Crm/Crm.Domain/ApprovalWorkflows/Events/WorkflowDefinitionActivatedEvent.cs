using SharedKernel;



namespace Crm.Domain.ApprovalWorkflows.Events;

public record WorkflowDefinitionActivatedEvent(Guid WorkflowDefinitionId) : IDomainEvent;