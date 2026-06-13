namespace CreditSystem.Application.Commands.RevolvingCredit.ChangeCreditLimit;

public record ChangeCreditLimitResponse
{
    public bool Success { get; init; }
    public Guid? CreditLineId { get; init; }
    public decimal? PreviousLimit { get; init; }
    public decimal? NewLimit { get; init; }
    public decimal? AvailableCredit { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static ChangeCreditLimitResponse Changed(
        Guid creditLineId,
        decimal previousLimit,
        decimal newLimit,
        decimal availableCredit) => new()
    {
        Success = true,
        CreditLineId = creditLineId,
        PreviousLimit = previousLimit,
        NewLimit = newLimit,
        AvailableCredit = availableCredit,
        Message = newLimit > previousLimit ? "Credit limit increased" : "Credit limit decreased"
    };

    public static ChangeCreditLimitResponse Failed(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };
}