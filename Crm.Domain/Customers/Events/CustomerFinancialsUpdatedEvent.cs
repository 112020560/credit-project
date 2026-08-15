using SharedKernel;



namespace Crm.Domain.Customers.Events;

public record CustomerFinancialsUpdatedEvent(Guid CustomerId) : IDomainEvent;