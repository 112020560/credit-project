using CreditSystem.Domain.Models;

namespace CreditSystem.Domain.Abstractions.Repositories;

public interface IUnderwritingPolicyRepository
{
    Task<UnderwritingPolicy> GetActiveAsync(CancellationToken ct = default);
}
