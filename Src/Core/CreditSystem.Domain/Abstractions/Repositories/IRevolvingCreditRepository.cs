using CreditSystem.Domain.Aggregates.RevolvingCredit;

namespace CreditSystem.Domain.Abstractions.Repositories;

public interface IRevolvingCreditRepository
{
    Task<RevolvingCreditAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(RevolvingCreditAggregate aggregate, CancellationToken ct = default);
}