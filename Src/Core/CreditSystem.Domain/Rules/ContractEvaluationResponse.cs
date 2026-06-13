namespace CreditSystem.Domain.Rules;

public record ContractEvaluationResponse
{
    public bool Approved { get; init; }
    public decimal InterestRate { get; init; }
    public IReadOnlyList<RuleEvaluationResult> Results { get; init; } = Array.Empty<RuleEvaluationResult>();

    public static ContractEvaluationResponse Approve(decimal rate, IReadOnlyList<RuleEvaluationResult> results) => new()
    {
        Approved = true,
        InterestRate = rate,
        Results = results
    };

    public static ContractEvaluationResponse Reject(IReadOnlyList<RuleEvaluationResult> results) => new()
    {
        Approved = false,
        InterestRate = 0,
        Results = results
    };
}