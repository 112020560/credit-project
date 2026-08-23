using Crm.Domain.Documents;

namespace Crm.Domain.Abstractions.Persistence;

public interface IDocumentsRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Document document, CancellationToken cancellationToken = default);
}
