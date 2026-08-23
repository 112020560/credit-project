using SharedKernel;



namespace Crm.Domain.RiskEngine.Events;

public record RiskMatrixActivatedEvent(Guid RiskMatrixId) : IDomainEvent;