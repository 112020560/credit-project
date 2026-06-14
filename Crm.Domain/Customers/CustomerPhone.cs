namespace Crm.Domain.Customers;

public partial class CustomerPhone
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string? CountryCode { get; set; }

    public string Number { get; set; } = null!;

    public string? Type { get; set; }

    public bool? IsPrimary { get; set; }

    public bool? Verified { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
