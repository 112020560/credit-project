using Crm.Domain.ApprovalWorkflows;

namespace Crm.Domain.Abstractions.Persistence;

public interface IApprovalDecisionsRepository
{
    Task AddAsync(ApprovalDecision decision, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalDecision>> GetByApplicationIdAsync(Guid creditApplicationId, CancellationToken cancellationToken = default);
}
