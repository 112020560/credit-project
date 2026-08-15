namespace CreditSystem.Api.EndPoints.Dtos;

public record PayoffContractRequest
{
    public string PaymentMethod { get; init; } = null!;
    public string? ReferenceNumber { get; init; }
}