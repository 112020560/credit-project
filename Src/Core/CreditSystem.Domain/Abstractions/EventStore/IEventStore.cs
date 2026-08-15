using CreditSystem.Domain.Abstractions.Events;

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

    /// <summary>
    /// Returns raw event rows with sequence > fromSequence ordered by sequence ASC.
    /// Used by ProjectionDispatcherWorker to feed projectors in stable insertion order.
    /// </summary>
    Task<IEnumerable<StoredEventRecord>> GetEventsSinceSequenceAsync(
        long fromSequence,
        int batchSize,
        CancellationToken ct = default);
}

/// <summary>Lightweight DTO for an event row returned to the projection dispatcher.</summary>
public record StoredEventRecord(
    Guid Id,
    Guid StreamId,
    string EventType,
    string EventData,
    long Sequence,
    DateTime StoredAt);