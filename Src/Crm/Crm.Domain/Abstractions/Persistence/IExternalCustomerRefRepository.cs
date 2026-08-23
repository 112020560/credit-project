using Crm.Domain.Customers;

namespace Crm.Domain.Abstractions.Persistence;

public interface IExternalCustomerRefRepository
{
    Task UpsertAsync(ExternalCustomerRef externalCustomerRef, CancellationToken cancellationToken);
    Task<ExternalCustomerRef?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken);
}
