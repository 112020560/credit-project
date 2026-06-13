namespace CreditSystem.Application.Commands.RevolvingCredit.CloseCreditLine;

public record CloseCreditLineResponse
{
    public bool Success { get; init; }
    public Guid? CreditLineId { get; init; }
    public DateTime? ClosedAt { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static CloseCreditLineResponse Closed(Guid creditLineId, DateTime closedAt) => new()
    {
        Success = true,
        CreditLineId = creditLineId,
        ClosedAt = closedAt,
        Message = "Credit line closed successfully"
    };

    public static CloseCreditLineResponse Failed(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };
}