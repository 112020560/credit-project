namespace CreditSystem.Application.Commands.SubmitRevolvingPayment;

/// <summary>
/// Response returned immediately after submitting a revolving payment for async processing.
/// </summary>
public record RevolvingPaymentAcceptedResponse
{
    public bool IsAccepted { get; init; }
    public Guid? PaymentId { get; init; }
    public Guid? CreditLineId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? TrackingUrl { get; init; }
    public DateTimeOffset? AcceptedAt { get; init; }
    public string? ErrorMessage { get; init; }

    public static RevolvingPaymentAcceptedResponse Accepted(
        Guid paymentId, Guid creditLineId, string trackingUrl) => new()
    {
        IsAccepted = true,
        PaymentId = paymentId,
        CreditLineId = creditLineId,
        Status = "PENDING",
        TrackingUrl = trackingUrl,
        AcceptedAt = DateTimeOffset.UtcNow
    };

    public static RevolvingPaymentAcceptedResponse Rejected(string errorMessage) => new()
    {
        IsAccepted = false,
        Status = "REJECTED",
        ErrorMessage = errorMessage
    };
}
