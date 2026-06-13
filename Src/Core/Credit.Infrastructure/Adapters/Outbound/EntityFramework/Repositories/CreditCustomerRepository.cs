using Credit.Application.Abstractions.Persistence;
using Credit.Domain.Entities;
using Credit.Infrastructure.Adapters.Outbound.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace Credit.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

internal sealed class CreditCustomerRepository(CreditDbContext context) : ICreditCustomerRepository
{
    public async Task<Customer?> GetByExternalIdAsync(Guid externalId, CancellationToken ct = default)
        => await context.Customers
            .FirstOrDefaultAsync(c => c.ExternalId == externalId.ToString(), ct);

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Customers.FindAsync([id], ct);
}
