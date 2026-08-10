namespace CreditSystem.Domain.Models;

public record IdempotencyRecord
{
    public Guid Key { get; init; }
    public int ResponseStatus { get; init; }
    public string ResponseBody { get; init; } = null!;
    public DateTime ExpiresAt { get; init; }
}
