using Crm.Domain.RiskEngine;

namespace Crm.Domain.Abstractions.Persistence;

public interface IRiskRulesRepository
{
    Task AddAsync(RiskRule rule, CancellationToken cancellationToken = default);
    Task<RiskRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RiskRule>> ListAllAsync(CancellationToken cancellationToken = default);
}
