using SharedKernel;



namespace Crm.Domain.CreditApplications.Events;

public record CreditApplicationRejectedEvent(Guid ApplicationId, Guid ProspectId, string? RejectionReason) : IDomainEvent;