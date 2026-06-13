namespace CreditSystem.Application.Commands.SubmitPayment;

/// <summary>
/// Response returned immediately after submitting a payment for async processing.
/// </summary>
public record PaymentAcceptedResponse
{
    public bool IsAccepted { get; init; }
    public Guid? PaymentId { get; init; }
    public Guid? LoanId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? TrackingUrl { get; init; }
    public DateTimeOffset? AcceptedAt { get; init; }
    public string? ErrorMessage { get; init; }

    public static PaymentAcceptedResponse Accepted(Guid paymentId, Guid loanId, string trackingUrl) => new()
    {
        IsAccepted = true,
        PaymentId = paymentId,
        LoanId = loanId,
        Status = "PENDING",
        TrackingUrl = trackingUrl,
        AcceptedAt = DateTimeOffset.UtcNow
    };

    public static PaymentAcceptedResponse Rejected(string errorMessage) => new()
    {
        IsAccepted = false,
        Status = "REJECTED",
        ErrorMessage = errorMessage
    };
}
