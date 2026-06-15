using Crm.Domain.Documents;

namespace Crm.Domain.Abstractions.Persistence;

public interface IDocumentTypesRepository
{
    Task<DocumentType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentType>> ListAllAsync(CancellationToken cancellationToken = default);
}
