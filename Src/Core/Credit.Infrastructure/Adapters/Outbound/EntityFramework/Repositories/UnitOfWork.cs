using Credit.Application.Abstractions.Persistence;
using Credit.Infrastructure.Adapters.Outbound.EntityFramework.Context;

namespace Credit.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

internal sealed class UnitOfWork(CreditDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
