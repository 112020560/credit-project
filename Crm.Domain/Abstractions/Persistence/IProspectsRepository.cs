using Crm.Domain.Prospects;

namespace Crm.Domain.Abstractions.Persistence;

public interface IProspectsRepository
{
    Task AddAsync(Prospect prospect, CancellationToken cancellationToken = default);
    Task<Prospect?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdentificationAsync(string identificationType, string identificationNumber, CancellationToken cancellationToken = default);
    Task UpdateAsync(Prospect prospect, CancellationToken cancellationToken = default);
}
