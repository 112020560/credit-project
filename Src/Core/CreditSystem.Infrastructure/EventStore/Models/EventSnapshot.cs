namespace CreditSystem.Infrastructure.EventStore.Models;

public class EventSnapshot
{
    public Guid Id { get; set; }
    public Guid StreamId { get; set; }
    public string StateData { get; set; } = null!;
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
}
