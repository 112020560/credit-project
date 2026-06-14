namespace Crm.Domain.Customers;

public partial class CustomerAddress
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string Type { get; set; } = null!;

    public string? Country { get; set; }

    public string? State { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string? Street { get; set; }

    public string? PostalCode { get; set; }

    public bool? IsPrimary { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
