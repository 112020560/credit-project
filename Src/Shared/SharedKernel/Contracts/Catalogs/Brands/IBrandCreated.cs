namespace SharedKernel.Contracts.Catalogs.Brands;

public interface IBrandCreated
{
    Guid BrandId { get; }
    string Name { get; }
    string? Description { get; }
    DateTimeOffset CreatedAt { get; }
}
