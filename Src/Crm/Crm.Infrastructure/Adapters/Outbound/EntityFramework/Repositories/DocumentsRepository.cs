using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

public class DocumentsRepository : IDocumentsRepository
{
    private readonly CrmDbContext _context;

    public DocumentsRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        await _context.Documents.AddAsync(document, cancellationToken);
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .AsTracking()
            .Include(d => d.Validations)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        // Entity is loaded with AsTracking(). DetectChanges() inside SaveChangesAsync
        // handles scalar property changes (Modified) and new collection items (Added).
        return Task.CompletedTask;
    }
}
