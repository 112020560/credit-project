using SharedKernel;

namespace Crm.Domain.RiskEngine;

public static class RiskRuleError
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("RiskRule.NotFound", $"Risk rule with Id '{id}' was not found");

    public static readonly Error InvalidWeight =
        Error.Problem("RiskRule.InvalidWeight", "Rule weight must be greater than zero");

    public static readonly Error UnknownType =
        Error.Problem("RiskRule.UnknownType", $"Rule type must be one of: {string.Join(", ", RuleType.All)}");
}
