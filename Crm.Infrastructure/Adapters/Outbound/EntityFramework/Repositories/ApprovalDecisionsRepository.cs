using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.ApprovalWorkflows;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

public class ApprovalDecisionsRepository : IApprovalDecisionsRepository
{
    private readonly CrmDbContext _context;

    public ApprovalDecisionsRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ApprovalDecision decision, CancellationToken cancellationToken = default)
    {
        await _context.ApprovalDecisions.AddAsync(decision, cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalDecision>> GetByApplicationIdAsync(Guid creditApplicationId, CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalDecisions
            .Where(d => d.CreditApplicationId == creditApplicationId)
            .ToListAsync(cancellationToken);
    }
}
