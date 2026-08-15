namespace CreditSystem.Application.Commands.RevolvingCredit.CreateCreditLine;

public record CreateCreditLineResponse
{
    public bool Success { get; init; }
    public Guid? CreditLineId { get; init; }
    public decimal? CreditLimit { get; init; }
    public decimal? InterestRate { get; init; }
    public int? BillingCycleDay { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }

    public static CreateCreditLineResponse Created(
        Guid id, 
        decimal limit, 
        decimal rate,
        int billingDay) => new()
    {
        Success = true,
        CreditLineId = id,
        CreditLimit = limit,
        InterestRate = rate,
        BillingCycleDay = billingDay,
        Message = "Credit line created successfully"
    };

    public static CreateCreditLineResponse Failed(string error) => new()
    {
        Success = false,
        Message = error,
        Errors = new[] { error }
    };

    public static CreateCreditLineResponse Failed(IEnumerable<string> errors) => new()
    {
        Success = false,
        Message = "Validation failed",
        Errors = errors.ToList()
    };
}