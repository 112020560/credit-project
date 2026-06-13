using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;

namespace CreditSystem.Domain.Abstractions.EventStore;

public interface IEventStore
{
    Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(
        Guid streamId, 
        int fromVersion = 0, 
        CancellationToken ct = default);
    
    Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(
        Guid streamId, 
        DateTime fromDate, 
        CancellationToken ct = default);
    
    Task AppendAsync(
        Guid streamId, 
        string streamType,
        IEnumerable<IDomainEvent> events, 
        int expectedVersion,
        CancellationToken ct = default);
    
    Task<int> GetCurrentVersionAsync(Guid streamId, CancellationToken ct = default);
    
    Task<bool> StreamExistsAsync(Guid streamId, CancellationToken ct = default);
    
    // Snapshots
    Task SaveSnapshotAsync<TState>(
        Guid streamId, 
        TState state, 
        int version,
        CancellationToken ct = default);
    
    Task<(TState? State, int Version)> GetLatestSnapshotAsync<TState>(
        Guid streamId,
        CancellationToken ct = default);
    
    // Queries globales
    Task<IReadOnlyList<IDomainEvent>> GetAllEventsAsync(
        string? eventType = null,
        DateTime? fromDate = null,
        int limit = 1000,
        CancellationToken ct = default);
}