using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Rules;
using CreditSystem.Domain.Rules.Implementations;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.Rules;

public class ProductEligibilityRuleTests
{
    private static CreditProduct MakeProduct(
        decimal minAmount = 100_000m,
        decimal maxAmount = 5_000_000m,
        int minTerm = 1,
        int maxTerm = 60,
        bool requiresCollateral = false,
        decimal? maxLtv = null,
        decimal? baseRate = 0.14m) =>
        new(
            Guid.NewGuid(),
            "Préstamo Personal",
            new ProductLimits(minAmount, maxAmount, minTerm, maxTerm),
            new ProductRates(baseRate, maxLtv),
            AmortizationMethod.French,
            requiresCollateral);

    private static ContractEvaluationContext BuildContext(
        decimal amount = 1_000_000m,
        int termMonths = 24,
        decimal? collateralValue = null,
        CreditProduct? product = null)
    {
        var customer = CustomerCreditProfile.Create(Guid.NewGuid(), "Test User", "CC", "123456");
        return new ContractEvaluationContext
        {
            Customer = customer,
            RequestedAmount = new Money(amount, "CRC"),
            TermMonths = termMonths,
            CollateralValue = collateralValue.HasValue ? new Money(collateralValue.Value, "CRC") : null,
            Product = product
        };
    }

    [Fact]
    public async Task EvaluateAsync_WhenProductIsNull_ShouldSkipRule()
    {
        var rule = new ProductEligibilityRule();
        var context = BuildContext(product: null);

        var result = await rule.EvaluateAsync(context);

        result.Passed.Should().BeTrue();
        result.Metadata.Should().ContainKey("Skipped");
    }

    [Fact]
    public async Task EvaluateAsync_WhenAmountWithinRange_ShouldPass()
    {
        var product = MakeProduct(minAmount: 100_000m, maxAmount: 5_000_000m);
        var rule = new ProductEligibilityRule();
        var context = BuildContext(amount: 1_000_000m, product: product);

        var result = await rule.EvaluateAsync(context);

        result.Passed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WhenAmountBelowMinimum_ShouldFail()
    {
        var product = MakeProduct(minAmount: 100_000m, maxAmount: 5_000_000m);
        var rule = new ProductEligibilityRule();
        var context = BuildContext(amount: 50_000m, product: product);

        var result = await rule.EvaluateAsync(context);

        result.Passed.Should().BeFalse();
        result.Message.Should().Contain("outside the allowed range");
    }

    [Fact]
    public async Task EvaluateAsync_WhenAmountAboveMaximum_ShouldFail()
    {
        var product = MakeProduct(minAmount: 100_000m, maxAmount: 5_000_000m);
        var rule = new ProductEligibilityRule();
        var context = BuildContext(amount: 6_000_000m, product: product);

        var result = await rule.EvaluateAsync(context);

        result.Passed.Should().BeFalse();
        result.Message.Should().Contain("outside the allowed range");
    }

    [Fact]
    public async Task EvaluateAsync_WhenTermBelowMinimum_ShouldFail()
    {
        var product = MakeProduct(minTerm: 12, maxTerm: 60);
        var rule = new ProductEligibilityRule();
        var context = BuildContext(termMonths: 6, product: product);

        var result = await rule.EvaluateAsync(context);

        result.Passed.Should().BeFalse();
        result.Message.Should().Contain("outside the allowed range");
    }

    [Fact]
    public async Task EvaluateAsync_WhenTermAboveMaximum_ShouldFail()
    {
        var product = MakeProduct(minTerm: 1, maxTerm: 60);
        var rule = new ProductEligibilityRule();
        var context = BuildContext(termMonths: 84, product: product);

        var result = await rule.EvaluateAsync(context);

        result.Passed.Should().BeFalse();
        result.Message.Should().Contain("outside the allowed range");
    }

    [Fact]
    public async Task EvaluateAsync_WhenCollateralRequiredButAbsent_ShouldFail()
    {
        var product = MakeProduct(requiresCollateral: true, maxLtv: 0.80m);
        var rule = new ProductEligibilityRule();
        var context = BuildContext(amount: 1_000_000m, collateralValue: null, product: product);

        var result = await rule.EvaluateAsync(context);

        result.Passed.Should().BeFalse();
        result.Message.Should().Contain("requires collateral");
    }

    [Fact]
    public async Task EvaluateAsync_WhenLtvExceedsMaximum_ShouldFail()
    {
        // Amount = 1,000,000 / Collateral = 1,000,000 → LTV = 1.0 > 0.80
        var product = MakeProduct(requiresCollateral: true, maxLtv: 0.80m);
        var rule = new ProductEligibilityRule();
        var context = BuildContext(amount: 1_000_000m, collateralValue: 1_000_000m, product: product);

        var result = await rule.EvaluateAsync(context);

        result.Passed.Should().BeFalse();
        result.Message.Should().Contain("LTV");
    }

    [Fact]
    public async Task EvaluateAsync_WhenLtvWithinLimit_ShouldPass()
    {
        // Amount = 800,000 / Collateral = 1,000,000 → LTV = 0.80 = MaxLtv = 0.80
        var product = MakeProduct(requiresCollateral: true, maxLtv: 0.80m);
        var rule = new ProductEligibilityRule();
        var context = BuildContext(amount: 800_000m, collateralValue: 1_000_000m, product: product);

        var result = await rule.EvaluateAsync(context);

        result.Passed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_WhenCollateralNotRequired_ShouldPassWithoutCollateral()
    {
        var product = MakeProduct(requiresCollateral: false);
        var rule = new ProductEligibilityRule();
        var context = BuildContext(amount: 1_000_000m, collateralValue: null, product: product);

        var result = await rule.EvaluateAsync(context);

        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void Priority_ShouldBeOne()
    {
        var rule = new ProductEligibilityRule();
        rule.Priority.Should().Be(1);
    }
}
