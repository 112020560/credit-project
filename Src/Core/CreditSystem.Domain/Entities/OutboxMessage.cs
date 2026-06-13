namespace CreditSystem.Domain.Entities;

/// <summary>
/// Represents a message to be published via the outbox pattern.
/// Ensures at-least-once delivery semantics for message publishing.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public Guid? CorrelationId { get; set; }
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}

public enum OutboxMessageStatus
{
    Pending,
    Published,
    Failed
}
