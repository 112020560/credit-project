using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.ApprovalWorkflows;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

public class WorkflowDefinitionsRepository : IWorkflowDefinitionsRepository
{
    private readonly CrmDbContext _context;

    public WorkflowDefinitionsRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        await _context.WorkflowDefinitions.AddAsync(definition, cancellationToken);
    }

    public async Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowDefinitions
            .Include(w => w.Steps.OrderBy(s => s.Order))
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public Task UpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        _context.WorkflowDefinitions.Update(definition);
        return Task.CompletedTask;
    }

    public async Task<WorkflowDefinition?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowDefinitions
            .Include(w => w.Steps.OrderBy(s => s.Order))
            .FirstOrDefaultAsync(w => w.Status == WorkflowStatus.Active, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowDefinitions
            .Where(w => w.Status == WorkflowStatus.Active)
            .ToListAsync(cancellationToken);
    }
}
