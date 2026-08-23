using SharedKernel;



namespace Crm.Domain.CreditApplications.Events;

public record CreditApplicationSentToReviewEvent(Guid ApplicationId, Guid ProspectId, Guid? WorkflowDefinitionId) : IDomainEvent;