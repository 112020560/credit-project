namespace CreditSystem.Application.Commands.DisburseLoan;

public record DisburseLoanResponse
{
    public bool Success { get; init; }
    public Guid? LoanId { get; init; }
    public decimal? AmountDisbursed { get; init; }
    public DateTime? DisbursedAt { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static DisburseLoanResponse Disbursed(Guid loanId, decimal amount, DateTime disbursedAt) => new()
    {
        Success = true,
        LoanId = loanId,
        AmountDisbursed = amount,
        DisbursedAt = disbursedAt,
        Message = "Loan disbursed successfully"
    };

    public static DisburseLoanResponse Failed(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };
}