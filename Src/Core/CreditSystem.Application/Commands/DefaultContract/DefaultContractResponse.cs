namespace CreditSystem.Application.Commands.DefaultContract;

public record DefaultContractResponse
{
    public bool Success { get; init; }
    public Guid? LoanId { get; init; }
    public decimal? OutstandingBalance { get; init; }
    public decimal? AccruedInterest { get; init; }
    public decimal? TotalOwed { get; init; }
    public DateTime? DefaultedAt { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static DefaultContractResponse Defaulted(
        Guid loanId,
        decimal outstandingBalance,
        decimal accruedInterest,
        decimal totalOwed,
        DateTime defaultedAt) => new()
    {
        Success = true,
        LoanId = loanId,
        OutstandingBalance = outstandingBalance,
        AccruedInterest = accruedInterest,
        TotalOwed = totalOwed,
        DefaultedAt = defaultedAt,
        Message = "Contract marked as defaulted"
    };

    public static DefaultContractResponse Failed(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };
}