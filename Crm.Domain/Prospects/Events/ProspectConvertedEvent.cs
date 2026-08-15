using SharedKernel;



namespace Crm.Domain.Prospects.Events;

public record ProspectConvertedEvent(Guid ProspectId) : IDomainEvent;