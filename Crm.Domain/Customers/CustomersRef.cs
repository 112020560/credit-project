namespace Crm.Domain.Customers;

public partial class CustomersRef
{
    public Guid Id { get; set; }

    public Guid ExternalId { get; set; }

    public string? DisplayName { get; set; }

    public string? LegalName { get; set; }

    public decimal? RiskScore { get; set; }

    public string? Metadata { get; set; }

    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
