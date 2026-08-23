using SharedKernel;



namespace Crm.Domain.Prospects.Events;

public record ProspectRejectedEvent(Guid ProspectId) : IDomainEvent;