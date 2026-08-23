using Crm.Domain.CreditApplications;

namespace Crm.Domain.Abstractions.Persistence;

public interface ICreditApplicationsRepository
{
    Task AddAsync(CreditApplication application, CancellationToken cancellationToken = default);
    Task<CreditApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CreditApplication>> GetByProspectIdAsync(Guid prospectId, CancellationToken cancellationToken = default);
    Task UpdateAsync(CreditApplication application, CancellationToken cancellationToken = default);
}
