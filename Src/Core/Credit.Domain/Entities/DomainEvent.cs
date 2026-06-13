namespace Credit.Domain.Entities;

public partial class DomainEvent
{
    public Guid Id { get; set; }

    public Guid? AggregateId { get; set; }

    public string? AggregateType { get; set; }

    public string? EventType { get; set; }

    public string? Payload { get; set; }

    public bool? Processed { get; set; }

    public DateTime CreatedAt { get; set; }
}
