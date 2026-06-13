namespace CreditSystem.Api.EndPoints.Dtos;

public record RestructureContractRequest
{
    public decimal NewInterestRate { get; init; }
    public int NewTermMonths { get; init; }
    public decimal? ForgiveAmount { get; init; }
    public string Reason { get; init; } = null!;
}