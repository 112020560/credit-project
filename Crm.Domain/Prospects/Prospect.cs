namespace Crm.Domain.Prospects;

public class Prospect
{
    public Guid Id { get; set; }
    public string IdentificationType { get; set; } = null!;
    public string IdentificationNumber { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? DisplayName { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string Status { get; set; } = ProspectStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ProspectAddress> Addresses { get; set; } = new List<ProspectAddress>();
    public virtual ICollection<ProspectPhone> Phones { get; set; } = new List<ProspectPhone>();
    public virtual ICollection<ProspectEmail> Emails { get; set; } = new List<ProspectEmail>();
    public virtual ICollection<ProspectWorkInfo> WorkInfos { get; set; } = new List<ProspectWorkInfo>();
    public virtual ICollection<ProspectFiscalInfo> FiscalInfos { get; set; } = new List<ProspectFiscalInfo>();
}
