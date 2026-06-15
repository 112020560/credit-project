namespace Crm.Domain.CreditApplications;

public class CreditApplication
{
    public Guid Id { get; set; }
    public Guid ProspectId { get; set; }
    public string Status { get; set; } = CreditApplicationStatus.Draft;
    public string? RejectionReason { get; set; }
    public Guid? WorkflowDefinitionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();
}
