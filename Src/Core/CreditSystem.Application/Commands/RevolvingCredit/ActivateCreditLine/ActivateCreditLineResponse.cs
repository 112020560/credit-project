namespace CreditSystem.Application.Commands.RevolvingCredit.ActivateCreditLine;

public record ActivateCreditLineResponse
{
    public bool Success { get; init; }
    public Guid? CreditLineId { get; init; }
    public DateTime? ActivatedAt { get; init; }
    public DateTime? NextStatementDate { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static ActivateCreditLineResponse Activated(
        Guid id, 
        DateTime activatedAt,
        DateTime nextStatementDate) => new()
    {
        Success = true,
        CreditLineId = id,
        ActivatedAt = activatedAt,
        NextStatementDate = nextStatementDate,
        Message = "Credit line activated successfully"
    };

    public static ActivateCreditLineResponse Failed(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };
}