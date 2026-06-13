using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;

namespace CreditSystem.Infrastructure.EventStore;

public interface IEventStoreTransaction : IAsyncDisposable
{
    Task AppendAsync(Guid streamId, string streamType, IEnumerable<IDomainEvent> events, int expectedVersion);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}