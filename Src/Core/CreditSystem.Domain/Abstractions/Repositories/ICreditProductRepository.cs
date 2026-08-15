using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;

namespace CreditSystem.Domain.Abstractions.Repositories;

public interface ICreditProductRepository
{
    Task<CreditProduct?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<CreditProduct>> GetAllActiveAsync(CancellationToken ct = default);
    Task<IEnumerable<CreditProduct>> GetAllAsync(CancellationToken ct = default);
    Task InsertAsync(CreditProduct product, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, ProductStatus status, CancellationToken ct = default);
}
