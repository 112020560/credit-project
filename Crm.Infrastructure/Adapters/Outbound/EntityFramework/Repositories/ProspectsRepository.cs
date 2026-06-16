using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Prospects;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

public class ProspectsRepository : IProspectsRepository
{
    private readonly CrmDbContext _context;

    public ProspectsRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Prospect prospect, CancellationToken cancellationToken = default)
    {
        await _context.Prospects.AddAsync(prospect, cancellationToken);
    }

    public async Task<Prospect?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Prospects
            .AsTracking()
            .Include(p => p.Addresses)
            .Include(p => p.Phones)
            .Include(p => p.Emails)
            .Include(p => p.WorkInfos)
            .Include(p => p.FiscalInfos)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByIdentificationAsync(string identificationType, string identificationNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Prospects.AnyAsync(
            p => p.IdentificationType == identificationType && p.IdentificationNumber == identificationNumber,
            cancellationToken);
    }

    public Task UpdateAsync(Prospect prospect, CancellationToken cancellationToken = default)
    {
        // Entity is loaded with AsTracking(). DetectChanges() inside SaveChangesAsync
        // handles scalar property changes (Modified) and new collection items (Added).
        return Task.CompletedTask;
    }
}
