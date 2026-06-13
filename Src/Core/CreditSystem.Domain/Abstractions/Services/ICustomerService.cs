using CreditSystem.Domain.Entities;

namespace CreditSystem.Domain.Abstractions.Services;

public interface ICustomerService
{
    Task<CustomerReference?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CustomerReference?> GetByExternalIdAsync(Guid externalId, CancellationToken ct = default);
    Task<CustomerReference?> GetByDocumentAsync(string documentNumber, CancellationToken ct = default);
}