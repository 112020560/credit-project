namespace CreditSystem.Infrastructure.EventStore.Models;

public class EventStream
{
    public Guid StreamId { get; set; }
    public string StreamType { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Version { get; set; }
    public bool IsDeleted { get; set; }
}