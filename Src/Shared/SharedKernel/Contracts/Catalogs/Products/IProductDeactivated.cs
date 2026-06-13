namespace SharedKernel.Contracts.Catalogs.Products;

public interface IProductDeactivated
{
    Guid ProductId { get; }
    string Sku { get; }
    DateTimeOffset DeactivatedAt { get; }
}
