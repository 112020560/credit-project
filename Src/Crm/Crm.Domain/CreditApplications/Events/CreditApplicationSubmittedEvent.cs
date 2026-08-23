using SharedKernel;



namespace Crm.Domain.CreditApplications.Events;

public record CreditApplicationSubmittedEvent(Guid ApplicationId, Guid ProspectId) : IDomainEvent;