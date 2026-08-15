namespace CreditSystem.Api.EndPoints.Dtos;

public record DisburseLoanRequest
{
    public string DisbursementMethod { get; init; } = null!;
    public string DestinationAccount { get; init; } = null!;
}