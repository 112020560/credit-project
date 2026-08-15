namespace CreditSystem.Application.Commands.RevolvingCredit.UnfreezeCreditLine;

public record UnfreezeCreditLineResponse
{
    public bool Success { get; init; }
    public Guid? CreditLineId { get; init; }
    public DateTime? UnfrozenAt { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static UnfreezeCreditLineResponse Unfrozen(Guid creditLineId, DateTime unfrozenAt) => new()
    {
        Success = true,
        CreditLineId = creditLineId,
        UnfrozenAt = unfrozenAt,
        Message = "Credit line unfrozen successfully"
    };

    public static UnfreezeCreditLineResponse Failed(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };
}