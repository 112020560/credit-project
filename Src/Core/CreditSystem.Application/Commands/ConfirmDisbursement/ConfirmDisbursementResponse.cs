namespace CreditSystem.Application.Commands.ConfirmDisbursement;

public record ConfirmDisbursementResponse
{
    public bool Success { get; init; }
    public Guid? LoanId { get; init; }
    public string? ConfirmedBy { get; init; }
    public DateTime? DisbursedAt { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static ConfirmDisbursementResponse Confirmed(Guid loanId, string confirmedBy, DateTime disbursedAt) => new()
    {
        Success = true,
        LoanId = loanId,
        ConfirmedBy = confirmedBy,
        DisbursedAt = disbursedAt,
        Message = "Disbursement confirmed successfully"
    };

    public static ConfirmDisbursementResponse Failed(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };
}
