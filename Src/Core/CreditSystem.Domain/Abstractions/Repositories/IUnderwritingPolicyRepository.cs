using CreditSystem.Domain.Models;

namespace CreditSystem.Domain.Abstractions.Repositories;

public interface IUnderwritingPolicyRepository
{
    Task<UnderwritingPolicy> GetActiveAsync(CancellationToken ct = default);
    Task<UnderwritingPolicy?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<UnderwritingPolicy>> GetAllAsync(CancellationToken ct = default);
}
