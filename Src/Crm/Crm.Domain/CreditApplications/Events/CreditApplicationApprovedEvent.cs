using SharedKernel;



namespace Crm.Domain.CreditApplications.Events;

public record CreditApplicationApprovedEvent(Guid ApplicationId, Guid ProspectId) : IDomainEvent;