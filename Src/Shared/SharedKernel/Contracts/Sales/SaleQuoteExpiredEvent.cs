namespace SharedKernel.Contracts.Sales;
public record SaleQuoteExpiredEvent
{
    public Guid QuoteId { get; init; }
    public string QuoteNumber { get; init; } = string.Empty;
    public DateTimeOffset ExpiredAt { get; init; }
}
