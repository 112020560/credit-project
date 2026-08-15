using CreditSystem.Domain.Models;

namespace CreditSystem.Domain.Abstractions.Repositories;

public interface IIdempotencyRepository
{
    Task<IdempotencyRecord?> FindAsync(Guid key, CancellationToken cancellationToken = default);
    Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken = default);
}
