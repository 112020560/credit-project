using Crm.Domain.RiskEngine;

namespace Crm.Domain.Abstractions.Persistence;

public interface IRiskEvaluationsRepository
{
    Task AddAsync(RiskEvaluation evaluation, CancellationToken cancellationToken = default);
    Task<RiskEvaluation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RiskEvaluation>> GetByCreditApplicationIdAsync(Guid creditApplicationId, CancellationToken cancellationToken = default);
}
