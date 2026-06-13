namespace SharedKernel.Contracts.Catalogs.Products;

public interface IProductCreated
{
    Guid ProductId { get; }
    string Sku { get; }
    string Name { get; }
    string? Description { get; }
    decimal Price { get; }
    string Currency { get; }
    Guid CategoryId { get; }
    Guid BrandId { get; }
    DateTimeOffset CreatedAt { get; }
}
