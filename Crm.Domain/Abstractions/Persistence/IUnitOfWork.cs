using System;

namespace Crm.Domain.Abstractions.Persistence;

public interface IUnitOfWork: IDisposable
{
    ICustomersRepository CustomersRepository { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
