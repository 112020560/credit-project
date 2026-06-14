namespace Crm.Domain.Customers;

public partial class CustomerEmail
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string Email { get; set; } = null!;

    public bool? IsPrimary { get; set; }

    public bool? Verified { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
