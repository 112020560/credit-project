namespace SharedKernel.Contracts.Catalogs.Products;

public interface IProductPriceChanged
{
    Guid ProductId { get; }
    string Sku { get; }
    decimal OldPrice { get; }
    string OldCurrency { get; }
    decimal NewPrice { get; }
    string NewCurrency { get; }
    DateTimeOffset ChangedAt { get; }
}
