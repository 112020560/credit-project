using SharedKernel;

namespace Crm.Domain.RiskEngine;

public static class RiskEvaluationError
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("RiskEvaluation.NotFound", $"Risk evaluation with Id '{id}' was not found");

    public static readonly Error NoActiveMatrix =
        Error.Problem("RiskEvaluation.NoActiveMatrix", "No active risk matrix found. Activate a matrix before evaluating applications");

    public static readonly Error ApplicationNotEligible =
        Error.Problem("RiskEvaluation.ApplicationNotEligible", "On-demand evaluation is only available for applications in InReview status");
}
