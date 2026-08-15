namespace CreditSystem.Application.Commands.FailDisbursement;

public record FailDisbursementResponse
{
    public bool Success { get; init; }
    public Guid? LoanId { get; init; }
    public string? Reason { get; init; }
    public DateTime? FailedAt { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static FailDisbursementResponse Failed(Guid loanId, string reason, DateTime failedAt) => new()
    {
        Success = true,
        LoanId = loanId,
        Reason = reason,
        FailedAt = failedAt,
        Message = "Disbursement failure recorded successfully"
    };

    public static FailDisbursementResponse Error(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };
}
