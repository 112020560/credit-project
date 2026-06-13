using Credit.Domain.Entities;

namespace Credit.Application.Abstractions.Persistence;

public interface ICreditCustomerRepository
{
    Task<Customer?> GetByExternalIdAsync(Guid externalId, CancellationToken ct = default);
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
