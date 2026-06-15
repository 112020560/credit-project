namespace Crm.Domain.ApprovalWorkflows;

public class ApprovalDecision
{
    public Guid Id { get; set; }
    public Guid CreditApplicationId { get; set; }
    public Guid? WorkflowDefinitionId { get; set; }
    public Guid? WorkflowStepId { get; set; }
    public string Decision { get; set; } = null!;
    public string? RejectionReason { get; set; }
    public string? DecidedBy { get; set; }
    public DateTime DecidedAt { get; set; }
}
