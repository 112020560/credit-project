namespace CreditSystem.Infrastructure.EventStore.Models;

public record EventMetadata
{
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
    public string? UserId { get; init; }
    public string? TenantId { get; init; }
    public string? IpAddress { get; init; }
    public Dictionary<string, string>? Custom { get; init; }
}