namespace CreditSystem.Domain.Rules;

public interface IContractRule
{
    string RuleName { get; }
    int Priority { get; }
    Task<RuleEvaluationResult> EvaluateAsync(ContractEvaluationContext context, CancellationToken ct = default);
}


public interface IHardStopRule : IContractRule
{
}