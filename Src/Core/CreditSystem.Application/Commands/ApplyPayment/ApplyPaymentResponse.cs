namespace CreditSystem.Application.Commands.ApplyPayment;

public record ApplyPaymentResponse
{
    public bool Success { get; init; }
    public Guid? PaymentId { get; init; }
    public Guid? LoanId { get; init; }
    public decimal? TotalApplied { get; init; }
    public decimal? PrincipalPaid { get; init; }
    public decimal? InterestPaid { get; init; }
    public decimal? FeesPaid { get; init; }
    public decimal? NewBalance { get; init; }
    public bool? IsPaidOff { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static ApplyPaymentResponse Applied(
        Guid paymentId,
        Guid loanId,
        decimal totalApplied,
        decimal principalPaid,
        decimal interestPaid,
        decimal feesPaid,
        decimal newBalance,
        bool isPaidOff) => new()
    {
        Success = true,
        PaymentId = paymentId,
        LoanId = loanId,
        TotalApplied = totalApplied,
        PrincipalPaid = principalPaid,
        InterestPaid = interestPaid,
        FeesPaid = feesPaid,
        NewBalance = newBalance,
        IsPaidOff = isPaidOff,
        Message = isPaidOff ? "Loan paid off successfully" : "Payment applied successfully"
    };

    public static ApplyPaymentResponse Failed(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };
}