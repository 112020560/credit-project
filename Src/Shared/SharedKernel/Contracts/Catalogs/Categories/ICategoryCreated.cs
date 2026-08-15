namespace SharedKernel.Contracts.Catalogs.Categories;

public interface ICategoryCreated
{
    Guid CategoryId { get; }
    string Name { get; }
    string? Description { get; }
    Guid? ParentCategoryId { get; }
    DateTimeOffset CreatedAt { get; }
}
