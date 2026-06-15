namespace Crm.Domain.Prospects;

public class ProspectAddress
{
    public Guid Id { get; set; }
    public Guid ProspectId { get; set; }
    public string? Type { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual Prospect Prospect { get; set; } = null!;
}
