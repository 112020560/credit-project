using CreditSystem.Domain.Models;

namespace CreditSystem.Domain.Rules.Implementations;

public class MemberSharesRule : IContractRule, IHardStopRule
{
    public string RuleName => "MemberSharesEvaluation";
    public int Priority => 0;

    private readonly UnderwritingPolicy _policy;

    public MemberSharesRule(UnderwritingPolicy policy)
    {
        _policy = policy;
    }

    public Task<RuleEvaluationResult> EvaluateAsync(
        ContractEvaluationContext context,
        CancellationToken ct = default)
    {
        // Validar membresía activa si la política lo requiere
        if (_policy.RequireActiveMembership && context.IsActiveMember != true)
        {
            return Task.FromResult(RuleEvaluationResult.Fail(
                RuleName,
                "Active cooperative membership required to apply for credit"));
        }

        // Si no hay datos de membresía o aportaciones = 0, omitir la regla
        if (context.MemberSharesAmount == null || context.MemberSharesAmount.Amount == 0)
        {
            return Task.FromResult(RuleEvaluationResult.Pass(
                RuleName,
                "No shares data available — rule skipped",
                new Dictionary<string, object> { ["Skipped"] = true }));
        }

        var maxAllowed = context.MemberSharesAmount.Amount * _policy.SharesMultiplierLimit;
        var requested = context.RequestedAmount.Amount;

        if (requested > maxAllowed)
        {
            return Task.FromResult(RuleEvaluationResult.Fail(
                RuleName,
                $"Requested amount {requested:N2} exceeds maximum allowed {maxAllowed:N2} " +
                $"({_policy.SharesMultiplierLimit}x member shares of {context.MemberSharesAmount.Amount:N2})"));
        }

        return Task.FromResult(RuleEvaluationResult.Pass(
            RuleName,
            $"Requested amount within shares limit ({requested:N2} <= {maxAllowed:N2})",
            new Dictionary<string, object>
            {
                ["SharesAmount"] = context.MemberSharesAmount.Amount,
                ["MultiplierLimit"] = _policy.SharesMultiplierLimit,
                ["MaxAllowedAmount"] = maxAllowed
            }));
    }
}
