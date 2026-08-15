namespace CreditSystem.Domain.Models;

public class ProjectionFailure
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid StreamId { get; set; }
    public string EventType { get; set; } = null!;
    public string ProjectorName { get; set; } = null!;
    public string ErrorMessage { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public int Attempts { get; set; }
    public bool Resolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
