namespace Crm.Domain.RiskEngine;

public static class RuleType
{
    public const string RangeCheck = "RangeCheck";
    public const string ThresholdCheck = "ThresholdCheck";
    public const string EnumCheck = "EnumCheck";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        RangeCheck,
        ThresholdCheck,
        EnumCheck
    };
}
