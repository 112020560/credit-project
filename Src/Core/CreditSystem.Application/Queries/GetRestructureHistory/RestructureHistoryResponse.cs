namespace CreditSystem.Application.Queries.GetRestructureHistory;

public record RestructureHistoryResponse
{
    public Guid EventId { get; init; }
    public DateTime RestructuredAt { get; init; }
    public decimal PreviousRate { get; init; }
    public decimal NewRate { get; init; }
    public int PreviousTermMonths { get; init; }
    public int NewTermMonths { get; init; }
    public decimal AmountForgiven { get; init; }
    public string Reason { get; init; } = null!;
}