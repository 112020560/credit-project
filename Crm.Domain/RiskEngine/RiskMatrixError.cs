using SharedKernel;

namespace Crm.Domain.RiskEngine;

public static class RiskMatrixError
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("RiskMatrix.NotFound", $"Risk matrix with Id '{id}' was not found");

    public static readonly Error OverlappingThresholds =
        Error.Problem("RiskMatrix.OverlappingThresholds", "AutoApproveThreshold must be greater than AutoRejectThreshold");

    public static readonly Error NoRules =
        Error.Problem("RiskMatrix.NoRules", "A risk matrix must contain at least one rule");

    public static readonly Error AlreadyActive =
        Error.Problem("RiskMatrix.AlreadyActive", "This matrix is already active");

    public static readonly Error NotEditable =
        Error.Problem("RiskMatrix.NotEditable", "Active or superseded matrices cannot be modified");

    public static readonly Error NotDraft =
        Error.Problem("RiskMatrix.NotDraft", "Only matrices in Draft status can be activated");

    public static readonly Error NoActiveMatrix =
        Error.Problem("RiskMatrix.NoActiveMatrix", "No active risk matrix found. Activate a matrix before submitting applications");
}
