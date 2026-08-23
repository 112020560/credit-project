using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.CreditApplications;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

public class CreditApplicationsRepository : ICreditApplicationsRepository
{
    private readonly CrmDbContext _context;

    public CreditApplicationsRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CreditApplication application, CancellationToken cancellationToken = default)
    {
        await _context.CreditApplications.AddAsync(application, cancellationToken);
    }

    public async Task<CreditApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CreditApplications
            .AsTracking()
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CreditApplication>> GetByProspectIdAsync(Guid prospectId, CancellationToken cancellationToken = default)
    {
        return await _context.CreditApplications
            .Include(a => a.Documents)
            .Where(a => a.ProspectId == prospectId)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(CreditApplication application, CancellationToken cancellationToken = default)
    {
        // Entity is loaded with AsTracking(). DetectChanges() inside SaveChangesAsync
        // handles scalar property changes (Modified) and new collection items (Added).
        return Task.CompletedTask;
    }
}
