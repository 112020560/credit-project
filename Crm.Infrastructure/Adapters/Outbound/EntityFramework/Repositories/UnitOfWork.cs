using System;
using Crm.Domain.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace Crm.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;

#nullable disable
public class UnitOfWork : IUnitOfWork
{
    private readonly CrmDbContext _dbContext;
    public ICustomersRepository _customersRepository;
    private IDbContextTransaction _transaction;
    public UnitOfWork(CrmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ICustomersRepository CustomersRepository => _customersRepository ??= new CustomersRepository(_dbContext);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _dbContext.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.CommitAsync(cancellationToken);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _dbContext?.Dispose();
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.RollbackAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
