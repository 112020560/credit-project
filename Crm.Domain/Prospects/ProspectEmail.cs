namespace Crm.Domain.Prospects;

public class ProspectEmail
{
    public Guid Id { get; set; }
    public Guid ProspectId { get; set; }
    public string? Email { get; set; }
    public bool IsPrimary { get; set; }
    public bool Verified { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Prospect Prospect { get; set; } = null!;
}
