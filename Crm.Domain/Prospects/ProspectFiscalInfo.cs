namespace Crm.Domain.Prospects;

public class ProspectFiscalInfo
{
    public Guid Id { get; set; }
    public Guid ProspectId { get; set; }
    public string? TaxId { get; set; }
    public string? TaxRegime { get; set; }
    public string? EconomicActivity { get; set; }
    public string? Industry { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual Prospect Prospect { get; set; } = null!;
}
