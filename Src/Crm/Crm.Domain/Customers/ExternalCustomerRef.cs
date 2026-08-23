namespace Crm.Domain.Customers;

/// <summary>
/// Represents a reference to a customer that was originated in an external system
/// and does not have a Customer record in this CRM. Used as an Anti-Corruption Layer
/// boundary — external systems write to this entity exclusively via
/// <c>POST /api/v1/external-customers</c>; no direct database writes are allowed.
/// </summary>
public class ExternalCustomerRef
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
