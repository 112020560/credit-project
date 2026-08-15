namespace CreditSystem.Application.Commands.RestructureContract;

public record RestructureContractResponse
{
    public bool Success { get; init; }
    public Guid? LoanId { get; init; }
    public decimal? PreviousRate { get; init; }
    public decimal? NewRate { get; init; }
    public int? PreviousTermMonths { get; init; }
    public int? NewTermMonths { get; init; }
    public decimal? AmountForgiven { get; init; }
    public decimal? NewBalance { get; init; }
    public decimal? NewMonthlyPayment { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static RestructureContractResponse Restructured(
        Guid loanId,
        decimal previousRate,
        decimal newRate,
        int previousTermMonths,
        int newTermMonths,
        decimal amountForgiven,
        decimal newBalance,
        decimal newMonthlyPayment) => new()
    {
        Success = true,
        LoanId = loanId,
        PreviousRate = previousRate,
        NewRate = newRate,
        PreviousTermMonths = previousTermMonths,
        NewTermMonths = newTermMonths,
        AmountForgiven = amountForgiven,
        NewBalance = newBalance,
        NewMonthlyPayment = newMonthlyPayment,
        Message = "Contract restructured successfully"
    };

    public static RestructureContractResponse Failed(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };
}