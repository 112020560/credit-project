namespace CreditSystem.Api.EndPoints.Dtos;

public record DefaultContractRequest
{
    public string Reason { get; init; } = null!;
}