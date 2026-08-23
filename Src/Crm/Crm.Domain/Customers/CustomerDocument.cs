namespace Crm.Domain.Customers;

public partial class CustomerDocument
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string Type { get; set; } = null!;

    public string StorageUrl { get; set; } = null!;

    public string? Metadata { get; set; }

    public DateTime UploadedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
