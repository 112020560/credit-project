using Crm.Domain.Abstractions.Persistence;
using Crm.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

public class DocumentTypesRepository : IDocumentTypesRepository
{
    private readonly CrmDbContext _context;

    public DocumentTypesRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentTypes
            .FirstOrDefaultAsync(dt => dt.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentType>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DocumentTypes.ToListAsync(cancellationToken);
    }
}
