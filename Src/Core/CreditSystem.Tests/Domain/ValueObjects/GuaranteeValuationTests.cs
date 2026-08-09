using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.ValueObjects;

public class GuaranteeValuationTests
{
    [Fact]
    public void EffectiveCoverage_ShouldBeAppraisalTimesRate()
    {
        var valuation = new GuaranteeValuation(10_000_000m, 0.80m);
        valuation.EffectiveCoverage.Should().Be(8_000_000m);
    }

    [Fact]
    public void Constructor_WithFullCoverageRate_ShouldWork()
    {
        var valuation = new GuaranteeValuation(5_000_000m, 1.0m);
        valuation.EffectiveCoverage.Should().Be(5_000_000m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveAppraisalValue_ShouldThrow(decimal appraisalValue)
    {
        var act = () => new GuaranteeValuation(appraisalValue, 0.80m);
        act.Should().Throw<ArgumentException>().WithParameterName("appraisalValue");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1.01)]
    [InlineData(2)]
    public void Constructor_WithInvalidCoverageRate_ShouldThrow(decimal coverageRate)
    {
        var act = () => new GuaranteeValuation(1_000_000m, coverageRate);
        act.Should().Throw<ArgumentException>().WithParameterName("coverageRate");
    }
}
