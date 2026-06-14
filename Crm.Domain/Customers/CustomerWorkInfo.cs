namespace Crm.Domain.Customers;

public partial class CustomerWorkInfo
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string? Occupation { get; set; }

    public string? EmployerName { get; set; }

    public decimal? Salary { get; set; }

    public string? WorkAddress { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
