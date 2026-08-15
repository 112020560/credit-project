namespace CreditSystem.Application.Commands.RevolvingCredit.ApplyRevolvingPayment;

public record ApplyRevolvingPaymentResponse
{
    public bool Success { get; init; }
    public Guid? PaymentId { get; init; }
    public Guid? CreditLineId { get; init; }
    public decimal? TotalPaid { get; init; }
    public decimal? PrincipalPaid { get; init; }
    public decimal? InterestPaid { get; init; }
    public decimal? FeesPaid { get; init; }
    public decimal? NewBalance { get; init; }
    public decimal? AvailableCredit { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static ApplyRevolvingPaymentResponse Applied(
        Guid paymentId,
        Guid creditLineId,
        decimal totalPaid,
        decimal principalPaid,
        decimal interestPaid,
        decimal feesPaid,
        decimal newBalance,
        decimal availableCredit) => new()
    {
        Success = true,
        PaymentId = paymentId,
        CreditLineId = creditLineId,
        TotalPaid = totalPaid,
        PrincipalPaid = principalPaid,
        InterestPaid = interestPaid,
        FeesPaid = feesPaid,
        NewBalance = newBalance,
        AvailableCredit = availableCredit,
        Message = "Payment applied successfully"
    };

    public static ApplyRevolvingPaymentResponse Failed(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };
}