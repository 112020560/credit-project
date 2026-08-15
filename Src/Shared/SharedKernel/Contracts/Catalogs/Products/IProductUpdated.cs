namespace SharedKernel.Contracts.Catalogs.Products;

public interface IProductUpdated
{
    Guid ProductId { get; }
    string Name { get; }
    string? Description { get; }
    Guid CategoryId { get; }
    Guid BrandId { get; }
    DateTimeOffset UpdatedAt { get; }
}
