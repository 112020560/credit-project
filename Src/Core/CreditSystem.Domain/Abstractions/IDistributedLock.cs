namespace CreditSystem.Domain.Abstractions;

public interface IDistributedLock
{
    Task<bool> TryAcquireAsync(long lockId, CancellationToken cancellationToken = default);
    Task ReleaseAsync(long lockId, CancellationToken cancellationToken = default);
}
