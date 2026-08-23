using SharedKernel;



namespace Crm.Domain.Prospects.Events;

public record ProspectSubmittedEvent(Guid ProspectId) : IDomainEvent;