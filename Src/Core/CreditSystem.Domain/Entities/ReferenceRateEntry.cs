namespace CreditSystem.Domain.Entities;

public class ReferenceRateEntry
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public decimal CurrentValue { get; init; }
    public DateTime EffectiveDate { get; init; }
    public string Source { get; init; } = null!;
    public DateTime UpdatedAt { get; init; }
}
