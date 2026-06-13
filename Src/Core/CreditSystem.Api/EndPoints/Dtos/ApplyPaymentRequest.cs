namespace CreditSystem.Api.EndPoints.Dtos;

public record ApplyPaymentRequest
{
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public string PaymentMethod { get; init; } = null!;
    public string? ReferenceNumber { get; init; }
}