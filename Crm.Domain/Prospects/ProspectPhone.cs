namespace Crm.Domain.Prospects;

public class ProspectPhone
{
    public Guid Id { get; set; }
    public Guid ProspectId { get; set; }
    public string? Type { get; set; }
    public string? Number { get; set; }
    public string? CountryCode { get; set; }
    public bool IsPrimary { get; set; }
    public bool Verified { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Prospect Prospect { get; set; } = null!;
}
