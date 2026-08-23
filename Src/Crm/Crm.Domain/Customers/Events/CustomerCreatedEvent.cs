using SharedKernel;



namespace Crm.Domain.Customers.Events;

public record CustomerCreatedEvent(Guid CustomerId) : IDomainEvent;