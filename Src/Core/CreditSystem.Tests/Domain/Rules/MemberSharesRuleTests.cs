using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Models;
using CreditSystem.Domain.Rules;
using CreditSystem.Domain.Rules.Implementations;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.Rules;

public class MemberSharesRuleTests
{
    private static UnderwritingPolicy DefaultPolicy(
        int multiplier = 5,
        bool requireMembership = false) =>
        new(8.0m, 90, NoScoreBehavior.ApproveWithPenalty, multiplier, requireMembership);

    private static ContractEvaluationContext BuildContext(
        decimal requestedAmount = 10000m,
        decimal? sharesAmount = 5000m,
        bool? isActiveMember = true,
        int multiplier = 5,
        bool requireMembership = false)
    {
        var customer = CustomerCreditProfile.Create(Guid.NewGuid(), "Test User", "CC", "123456");
        return new ContractEvaluationContext
        {
            Customer = customer,
            RequestedAmount = new Money(requestedAmount, "CRC"),
            TermMonths = 12,
            MemberSharesAmount = sharesAmount.HasValue ? new Money(sharesAmount.Value, "CRC") : null,
            IsActiveMember = isActiveMember,
            Policy = DefaultPolicy(multiplier, requireMembership)
        };
    }

    [Fact]
    public async Task EvaluateAsync_WhenAmountWithinLimit_ShouldPass()
    {
        // 10,000 <= 5,000 × 5 = 25,000
        var rule = new MemberSharesRule();
        var context = BuildContext(requestedAmount: 10_000m, sharesAmount: 5_000m, multiplier: 5);

        var result = await rule.EvaluateAsync(context);

        result.Passed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WhenAmountExceedsLimit_ShouldFail()
    {
        // 30,000 > 5,000 × 5 = 25,000
        var rule = new MemberSharesRule();
        var context = BuildContext(requestedAmount: 30_000m, sharesAmount: 5_000m, multiplier: 5);

        var result = await rule.EvaluateAsync(context);

        result.Passed.Should().BeFalse();
        result.Message.Should().Contain("exceeds maximum allowed");
    }

    [Fact]
    public async Task EvaluateAsync_WhenNoSharesData_ShouldSkipRule()
    {
        var rule = new MemberSharesRule();
        var context = BuildContext(requestedAmount: 100_000m, sharesAmount: null);

        var result = await rule.EvaluateAsync(context);

        result.Passed.Should().BeTrue();
        result.Metadata.Should().ContainKey("Skipped");
    }

    [Fact]
    public async Task EvaluateAsync_WhenSharesAmountIsZero_ShouldSkipRule()
    {
        var rule = new MemberSharesRule();
        var context = BuildContext(requestedAmount: 100_000m, sharesAmount: 0m);

        var result = await rule.EvaluateAsync(context);

        result.Passed.Should().BeTrue();
        result.Metadata.Should().ContainKey("Skipped");
    }

    [Fact]
    public async Task EvaluateAsync_WhenMembershipInactiveAndPolicyRestrictive_ShouldFail()
    {
        var rule = new MemberSharesRule();
        var context = BuildContext(isActiveMember: false, sharesAmount: 50_000m, requireMembership: true);

        var result = await rule.EvaluateAsync(context);

        result.Passed.Should().BeFalse();
        result.Message.Should().Contain("membership required");
    }

    [Fact]
    public async Task EvaluateAsync_WhenMembershipInactiveAndPolicyPermissive_ShouldSkip()
    {
        var rule = new MemberSharesRule();
        var context = BuildContext(isActiveMember: false, sharesAmount: 5_000m, requestedAmount: 10_000m, requireMembership: false);

        var result = await rule.EvaluateAsync(context);

        // La regla de membresía no bloquea, pero la regla de aportaciones sí corre
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Priority_ShouldBeZero()
    {
        var rule = new MemberSharesRule();
        rule.Priority.Should().Be(0);
    }
}
