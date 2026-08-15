namespace SharedKernel.Contracts.Sales;
public record SaleQuoteCancelledEvent
{
    public Guid QuoteId { get; init; }
    public string QuoteNumber { get; init; } = string.Empty;
    public DateTimeOffset CancelledAt { get; init; }
}
