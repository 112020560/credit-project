namespace CreditSystem.Domain.Models;

public record AuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string? UserId { get; init; }
    public string Action { get; init; } = null!;
    public string EntityType { get; init; } = null!;
    public Guid EntityId { get; init; }
    public object? Details { get; init; }
}
