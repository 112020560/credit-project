namespace Crm.Domain.ApprovalWorkflows;

public class WorkflowDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Status { get; set; } = WorkflowStatus.Draft;
    public DateTime CreatedAt { get; set; }

    public virtual ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
}
