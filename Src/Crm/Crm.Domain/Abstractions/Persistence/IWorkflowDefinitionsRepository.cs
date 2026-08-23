using Crm.Domain.ApprovalWorkflows;

namespace Crm.Domain.Abstractions.Persistence;

public interface IWorkflowDefinitionsRepository
{
    Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition?> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowDefinition>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
