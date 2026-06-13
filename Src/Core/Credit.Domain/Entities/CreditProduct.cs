namespace Credit.Domain.Entities;

public partial class CreditProduct
{
    public Guid Id { get; set; }

    public string? Code { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Currency { get; set; } = null!;

    public decimal InterestRate { get; set; }

    public string InterestType { get; set; } = null!;

    public string AmortizationMethod { get; set; } = null!;

    public int TermMonths { get; set; }

    public decimal? MinAmount { get; set; }

    public decimal? MaxAmount { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CreditApplication> CreditApplications { get; set; } = new List<CreditApplication>();

    public virtual ICollection<CreditLine> CreditLines { get; set; } = new List<CreditLine>();
}
