using Crm.Domain.CreditApplications.Events;
using SharedKernel;

namespace Crm.Domain.CreditApplications;

public class CreditApplication : AggregateRoot
{
    public Guid Id { get; set; }
    public Guid ProspectId { get; set; }
    public string Status { get; set; } = CreditApplicationStatus.Draft;
    public string? RejectionReason { get; set; }
    public Guid? WorkflowDefinitionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();

    public Result Submit()
    {
        if (Status != CreditApplicationStatus.Draft)
            return Result.Failure(CreditApplicationError.InvalidTransition(Status, CreditApplicationStatus.Submitted));

        Status = CreditApplicationStatus.Submitted;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CreditApplicationSubmittedEvent(Id, ProspectId));
        return Result.Success();
    }

    public Result Approve()
    {
        if (Status != CreditApplicationStatus.Submitted && Status != CreditApplicationStatus.InReview)
            return Result.Failure(CreditApplicationError.InvalidTransition(Status, CreditApplicationStatus.Approved));

        Status = CreditApplicationStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CreditApplicationApprovedEvent(Id, ProspectId));
        return Result.Success();
    }

    public Result Reject(string? reason)
    {
        if (Status != CreditApplicationStatus.Submitted && Status != CreditApplicationStatus.InReview)
            return Result.Failure(CreditApplicationError.InvalidTransition(Status, CreditApplicationStatus.Rejected));

        Status = CreditApplicationStatus.Rejected;
        RejectionReason = reason;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CreditApplicationRejectedEvent(Id, ProspectId, reason));
        return Result.Success();
    }

    public Result SendToReview(Guid? workflowDefinitionId)
    {
        if (Status != CreditApplicationStatus.Submitted)
            return Result.Failure(CreditApplicationError.InvalidTransition(Status, CreditApplicationStatus.InReview));

        Status = CreditApplicationStatus.InReview;
        WorkflowDefinitionId = workflowDefinitionId;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CreditApplicationSentToReviewEvent(Id, ProspectId, workflowDefinitionId));
        return Result.Success();
    }
}
