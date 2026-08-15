namespace CreditSystem.Infrastructure.EventStore.Models;

public class StoredEvent
{
    public Guid Id { get; set; }
    public Guid StreamId { get; set; }
    public string EventType { get; set; } = null!;
    public string EventData { get; set; } = null!;
    public string? Metadata { get; set; }
    public int Version { get; set; }
    public string Hash { get; set; } = null!;
    public string? PreviousHash { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime StoredAt { get; set; }
}