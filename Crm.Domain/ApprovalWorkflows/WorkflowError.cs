using SharedKernel;

namespace Crm.Domain.ApprovalWorkflows;

public static class WorkflowError
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Workflow.NotFound", $"Workflow definition with Id '{id}' was not found");

    public static readonly Error AlreadyActive =
        Error.Problem("Workflow.AlreadyActive", "This workflow definition is already active");

    public static readonly Error NoSteps =
        Error.Problem("Workflow.NoSteps", "A workflow definition must have at least one step");
}
