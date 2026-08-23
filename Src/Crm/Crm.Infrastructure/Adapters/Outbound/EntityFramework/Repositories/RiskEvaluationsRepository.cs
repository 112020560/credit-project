using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.RiskEngine;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

public class RiskEvaluationsRepository : IRiskEvaluationsRepository
{
    private readonly CrmDbContext _context;

    public RiskEvaluationsRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RiskEvaluation evaluation, CancellationToken cancellationToken = default)
    {
        await _context.RiskEvaluations.AddAsync(evaluation, cancellationToken);
    }

    public async Task<RiskEvaluation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RiskEvaluations
            .Include(e => e.Entries)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RiskEvaluation>> GetByCreditApplicationIdAsync(Guid creditApplicationId, CancellationToken cancellationToken = default)
    {
        return await _context.RiskEvaluations
            .Include(e => e.Entries)
            .Where(e => e.CreditApplicationId == creditApplicationId)
            .ToListAsync(cancellationToken);
    }
}
