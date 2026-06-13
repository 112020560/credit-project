namespace SharedKernel.Contracts.Sales;

public record SaleQuoteCreatedEvent
{
    public Guid QuoteId { get; init; }
    public string QuoteNumber { get; init; } = string.Empty;
    public Guid WarehouseId { get; init; }
    public IReadOnlyList<SaleQuoteLineContract> Lines { get; init; } = [];
    public DateTimeOffset ValidUntil { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public record SaleQuoteLineContract(Guid ProductId, decimal Quantity);
