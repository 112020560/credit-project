namespace Crm.Domain.Prospects;

public class ProspectWorkInfo
{
    public Guid Id { get; set; }
    public Guid ProspectId { get; set; }
    public string? Occupation { get; set; }
    public string? EmployerName { get; set; }
    public decimal? Salary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual Prospect Prospect { get; set; } = null!;
}
