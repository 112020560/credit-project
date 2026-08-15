namespace CreditSystem.Infrastructure.EventStore.Models;

/// <summary>
/// Lightweight projection of a stored_events row used by ProjectionDispatcherWorker.
/// Contains only the fields needed for dispatching events to projectors.
/// </summary>
public class StoredEventRow
{
    public Guid Id { get; set; }
    public Guid StreamId { get; set; }
    public string EventType { get; set; } = null!;
    public string EventData { get; set; } = null!;
    public long Sequence { get; set; }
    public DateTime StoredAt { get; set; }
}
