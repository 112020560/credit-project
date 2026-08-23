using SharedKernel;



namespace Crm.Domain.Customers.Events;

public record CustomerDeactivatedEvent(Guid CustomerId) : IDomainEvent;