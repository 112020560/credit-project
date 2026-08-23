using SharedKernel;



namespace Crm.Domain.Customers.Events;

public record CustomerActivatedEvent(Guid CustomerId) : IDomainEvent;