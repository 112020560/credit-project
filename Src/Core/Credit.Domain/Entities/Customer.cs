namespace Credit.Domain.Entities;

public partial class Customer
{
    public Guid Id { get; set; }

    public string? ExternalId { get; set; }

    public string DisplayName { get; set; } = null!;

    public string? LegalName { get; set; }

    public string? TaxId { get; set; }

    public List<string>? Emails { get; set; }

    public List<string>? Roles { get; set; }

    public string? Address { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CreditApplication> CreditApplications { get; set; } = new List<CreditApplication>();

    public virtual ICollection<CreditLine> CreditLines { get; set; } = new List<CreditLine>();
}
