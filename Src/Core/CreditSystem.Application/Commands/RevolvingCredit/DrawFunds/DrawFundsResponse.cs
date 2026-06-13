namespace CreditSystem.Application.Commands.RevolvingCredit.DrawFunds;

public record DrawFundsResponse
{
    public bool Success { get; init; }
    public Guid? DrawId { get; init; }
    public Guid? CreditLineId { get; init; }
    public decimal? AmountDrawn { get; init; }
    public decimal? NewBalance { get; init; }
    public decimal? AvailableCredit { get; init; }
    public DateTime? DrawnAt { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static DrawFundsResponse Drawn(
        Guid drawId,
        Guid creditLineId,
        decimal amount,
        decimal newBalance,
        decimal availableCredit,
        DateTime drawnAt) => new()
    {
        Success = true,
        DrawId = drawId,
        CreditLineId = creditLineId,
        AmountDrawn = amount,
        NewBalance = newBalance,
        AvailableCredit = availableCredit,
        DrawnAt = drawnAt,
        Message = "Funds drawn successfully"
    };

    public static DrawFundsResponse Failed(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };
}