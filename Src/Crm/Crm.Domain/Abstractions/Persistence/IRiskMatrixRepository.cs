using Crm.Domain.RiskEngine;

namespace Crm.Domain.Abstractions.Persistence;

public interface IRiskMatrixRepository
{
    Task AddAsync(RiskMatrix matrix, CancellationToken cancellationToken = default);
    Task<RiskMatrix?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RiskMatrix?> GetActiveAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(RiskMatrix matrix, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RiskMatrix>> ListAsync(CancellationToken cancellationToken = default);
}
