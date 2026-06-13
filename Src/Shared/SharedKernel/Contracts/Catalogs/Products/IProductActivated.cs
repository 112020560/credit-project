namespace SharedKernel.Contracts.Catalogs.Products;

public interface IProductActivated
{
    Guid ProductId { get; }
    string Sku { get; }
    DateTimeOffset ActivatedAt { get; }
}
