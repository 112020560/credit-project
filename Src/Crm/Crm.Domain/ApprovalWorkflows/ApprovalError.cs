using SharedKernel;

namespace Crm.Domain.ApprovalWorkflows;

public static class ApprovalError
{
    public static readonly Error ApplicationNotInReview =
        Error.Problem("Approval.ApplicationNotInReview", "The credit application is not in InReview status");

    public static readonly Error RejectionReasonRequired =
        Error.Problem("Approval.RejectionReasonRequired", "A rejection reason is required when rejecting an application");
}
