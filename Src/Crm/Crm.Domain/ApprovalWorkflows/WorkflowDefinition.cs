using Crm.Domain.ApprovalWorkflows.Events;
using SharedKernel;

namespace Crm.Domain.ApprovalWorkflows;

public class WorkflowDefinition : AggregateRoot
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Status { get; set; } = WorkflowStatus.Draft;
    public DateTime CreatedAt { get; set; }

    public virtual ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();

    public Result Activate()
    {
        Status = WorkflowStatus.Active;
        RaiseDomainEvent(new WorkflowDefinitionActivatedEvent(Id));
        return Result.Success();
    }

    public Result Deactivate()
    {
        Status = WorkflowStatus.Superseded;
        return Result.Success();
    }
}
