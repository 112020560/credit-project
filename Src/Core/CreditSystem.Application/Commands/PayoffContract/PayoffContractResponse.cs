namespace CreditSystem.Application.Commands.PayoffContract;

public record PayoffContractResponse
{
    public bool Success { get; init; }
    public Guid? LoanId { get; init; }
    public Guid? PaymentId { get; init; }
    public decimal? PayoffAmount { get; init; }
    public decimal? PrincipalPaid { get; init; }
    public decimal? InterestPaid { get; init; }
    public decimal? FeesPaid { get; init; }
    public decimal? TotalPrincipalPaid { get; init; }
    public decimal? TotalInterestPaid { get; init; }
    public decimal? TotalFeesPaid { get; init; }
    public bool? EarlyPayoff { get; init; }
    public DateTime? PaidOffAt { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static PayoffContractResponse PaidOff(
        Guid loanId,
        Guid paymentId,
        decimal payoffAmount,
        decimal principalPaid,
        decimal interestPaid,
        decimal feesPaid,
        decimal totalPrincipalPaid,
        decimal totalInterestPaid,
        decimal totalFeesPaid,
        bool earlyPayoff,
        DateTime paidOffAt) => new()
    {
        Success = true,
        LoanId = loanId,
        PaymentId = paymentId,
        PayoffAmount = payoffAmount,
        PrincipalPaid = principalPaid,
        InterestPaid = interestPaid,
        FeesPaid = feesPaid,
        TotalPrincipalPaid = totalPrincipalPaid,
        TotalInterestPaid = totalInterestPaid,
        TotalFeesPaid = totalFeesPaid,
        EarlyPayoff = earlyPayoff,
        PaidOffAt = paidOffAt,
        Message = earlyPayoff ? "Loan paid off early" : "Loan paid off successfully"
    };

    public static PayoffContractResponse Failed(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };
}