using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.RiskEngine;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

public class RiskRulesRepository : IRiskRulesRepository
{
    private readonly CrmDbContext _context;

    public RiskRulesRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RiskRule rule, CancellationToken cancellationToken = default)
    {
        await _context.RiskRules.AddAsync(rule, cancellationToken);
    }

    public async Task<RiskRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RiskRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RiskRule>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RiskRules.ToListAsync(cancellationToken);
    }
}
