using SharedKernel;



namespace Crm.Domain.Customers.Events;

public record CustomerProfileUpdatedEvent(Guid CustomerId) : IDomainEvent;