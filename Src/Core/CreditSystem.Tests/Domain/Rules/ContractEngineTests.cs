using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Models;
using CreditSystem.Domain.Rules;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.Rules;

public class ContractEngineTests
{
    private static UnderwritingPolicy DefaultPolicy(decimal baseRate = 8.0m) =>
        new(baseRate, 90, NoScoreBehavior.ApproveWithPenalty, 5, false);

    private static ContractEvaluationContext BuildContext(decimal amount = 100_000m) =>
        new()
        {
            Customer = CustomerCreditProfile.Create(Guid.NewGuid(), "Test User", "CC", "123456"),
            RequestedAmount = new Money(amount, "CRC"),
            TermMonths = 12
        };

    // A rule that always passes with no rate adjustment
    private sealed class PassRule : IContractRule
    {
        public string RuleName => "AlwaysPass";
        public int Priority => 10;
        public Task<RuleEvaluationResult> EvaluateAsync(ContractEvaluationContext ctx, CancellationToken ct = default)
            => Task.FromResult(RuleEvaluationResult.Pass(RuleName, "OK"));
    }

    [Fact]
    public async Task EvaluateAsync_WithoutBaseRateParam_UsesPolicy()
    {
        // Policy base rate = 8%, no rules add adjustments
        var engine = new ContractEngine([new PassRule()], DefaultPolicy(baseRate: 8.0m));
        var context = BuildContext();

        var result = await engine.EvaluateAsync(context);

        result.Approved.Should().BeTrue();
        result.InterestRate.Should().Be(8.0m);
    }

    [Fact]
    public async Task EvaluateAsync_WithProductBaseRate_OverridesPolicyRate()
    {
        // Policy base rate = 8%, product base rate = 14%
        var engine = new ContractEngine([new PassRule()], DefaultPolicy(baseRate: 8.0m));
        var context = BuildContext();

        var result = await engine.EvaluateAsync(context, baseInterestRate: 14.0m);

        result.Approved.Should().BeTrue();
        result.InterestRate.Should().Be(14.0m);
    }

    [Fact]
    public async Task EvaluateAsync_WithNullBaseRate_FallsBackToPolicy()
    {
        var engine = new ContractEngine([new PassRule()], DefaultPolicy(baseRate: 9.0m));
        var context = BuildContext();

        var result = await engine.EvaluateAsync(context, baseInterestRate: null);

        result.Approved.Should().BeTrue();
        result.InterestRate.Should().Be(9.0m);
    }

    [Fact]
    public async Task EvaluateAsync_ProductBaseRateAccumulatesRuleAdjustments()
    {
        // Product base = 12%, rule adds +2% → final = 14%
        var ruleWithAdjustment = new RateAdjustmentRule(2.0m);
        var engine = new ContractEngine([ruleWithAdjustment], DefaultPolicy(baseRate: 8.0m));
        var context = BuildContext();

        var result = await engine.EvaluateAsync(context, baseInterestRate: 12.0m);

        result.Approved.Should().BeTrue();
        result.InterestRate.Should().Be(14.0m);
    }

    // Helper rule that passes and adds a rate adjustment
    private sealed class RateAdjustmentRule(decimal adjustment) : IContractRule
    {
        public string RuleName => "RateAdjustment";
        public int Priority => 10;
        public Task<RuleEvaluationResult> EvaluateAsync(ContractEvaluationContext ctx, CancellationToken ct = default)
            => Task.FromResult(RuleEvaluationResult.Pass(
                RuleName, "Adjustment applied",
                new Dictionary<string, object> { ["RateAdjustment"] = adjustment }));
    }
}
