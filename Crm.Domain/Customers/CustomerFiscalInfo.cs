namespace Crm.Domain.Customers;

public partial class CustomerFiscalInfo
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string? TaxId { get; set; }

    public string? TaxRegime { get; set; }

    public string? EconomicActivity { get; set; }

    public string? Industry { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
