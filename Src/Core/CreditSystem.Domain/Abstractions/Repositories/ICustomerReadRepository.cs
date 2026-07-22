using CreditSystem.Domain.Entities;

namespace CreditSystem.Domain.Abstractions.Repositories;

public interface ICustomerReadRepository
{
    Task<CustomerCreditProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CustomerCreditProfile?> GetByExternalIdAsync(Guid externalId, CancellationToken ct = default);
    Task<CustomerCreditProfile?> GetByDocumentAsync(string documentNumber, CancellationToken ct = default);
}
