namespace CreditSystem.Application.Commands.RevolvingCredit.FreezeCreditLine;

public record FreezeCreditLineResponse
{
    public bool Success { get; init; }
    public Guid? CreditLineId { get; init; }
    public DateTime? FrozenAt { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static FreezeCreditLineResponse Frozen(Guid creditLineId, DateTime frozenAt) => new()
    {
        Success = true,
        CreditLineId = creditLineId,
        FrozenAt = frozenAt,
        Message = "Credit line frozen successfully"
    };

    public static FreezeCreditLineResponse Failed(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };
}