using CreditSystem.Domain.Entities;

namespace CreditSystem.Domain.Abstractions.Repositories;

public interface IReferenceRateRepository
{
    Task<ReferenceRateEntry?> GetCurrentAsync(string id, CancellationToken cancellationToken = default);
    Task UpsertAsync(ReferenceRateEntry entry, CancellationToken cancellationToken = default);
}
