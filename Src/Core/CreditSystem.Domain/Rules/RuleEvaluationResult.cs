namespace CreditSystem.Domain.Rules;

public record RuleEvaluationResult
{
    public bool Passed { get; init; }
    public string RuleName { get; init; } = null!;
    public string Message { get; init; } = null!;
    public Dictionary<string, object>? Metadata { get; init; }

    public static RuleEvaluationResult Pass(string ruleName, string message, Dictionary<string, object>? metadata = null) => new()
    {
        Passed = true,
        RuleName = ruleName,
        Message = message,
        Metadata = metadata
    };

    public static RuleEvaluationResult Fail(string ruleName, string message, Dictionary<string, object>? metadata = null) => new()
    {
        Passed = false,
        RuleName = ruleName,
        Message = message,
        Metadata = metadata
    };
}