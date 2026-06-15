namespace Crm.Domain.ApprovalWorkflows;

public class WorkflowStep
{
    public Guid Id { get; set; }
    public Guid WorkflowDefinitionId { get; set; }
    public string StepName { get; set; } = null!;
    public int Order { get; set; }
    public string? RequiredRole { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual WorkflowDefinition WorkflowDefinition { get; set; } = null!;
}
