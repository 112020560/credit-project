using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.RiskEngine;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

public class RiskMatrixRepository : IRiskMatrixRepository
{
    private readonly CrmDbContext _context;

    public RiskMatrixRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RiskMatrix matrix, CancellationToken cancellationToken = default)
    {
        await _context.RiskMatrices.AddAsync(matrix, cancellationToken);
    }

    public async Task<RiskMatrix?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RiskMatrices
            .Include(m => m.MatrixRules)
                .ThenInclude(mr => mr.RiskRule)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<RiskMatrix?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RiskMatrices
            .Include(m => m.MatrixRules)
                .ThenInclude(mr => mr.RiskRule)
            .FirstOrDefaultAsync(m => m.Status == RiskMatrixStatus.Active, cancellationToken);
    }

    public Task UpdateAsync(RiskMatrix matrix, CancellationToken cancellationToken = default)
    {
        _context.RiskMatrices.Update(matrix);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<RiskMatrix>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RiskMatrices.ToListAsync(cancellationToken);
    }
}
