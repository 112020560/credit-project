using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Services;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.Services;

public class RiskCategoryTableTests
{
    // ── GetCategory thresholds ──────────────────────────────────────────────

    [Theory]
    [InlineData(0, LoanRiskCategory.A1)]
    [InlineData(1, LoanRiskCategory.A2)]
    [InlineData(30, LoanRiskCategory.A2)]
    [InlineData(31, LoanRiskCategory.B1)]
    [InlineData(60, LoanRiskCategory.B1)]
    [InlineData(61, LoanRiskCategory.B2)]
    [InlineData(90, LoanRiskCategory.B2)]
    [InlineData(91, LoanRiskCategory.C1)]
    [InlineData(120, LoanRiskCategory.C1)]
    [InlineData(121, LoanRiskCategory.C2)]
    [InlineData(180, LoanRiskCategory.C2)]
    [InlineData(181, LoanRiskCategory.D)]
    [InlineData(360, LoanRiskCategory.D)]
    [InlineData(361, LoanRiskCategory.E)]
    [InlineData(999, LoanRiskCategory.E)]
    public void GetCategory_ReturnsCorrectCategory(int daysOverdue, LoanRiskCategory expected)
    {
        RiskCategoryTable.GetCategory(daysOverdue).Should().Be(expected);
    }

    // ── GetProvisionRate percentages ────────────────────────────────────────

    [Theory]
    [InlineData(LoanRiskCategory.A1, 0.000)]
    [InlineData(LoanRiskCategory.A2, 0.005)]
    [InlineData(LoanRiskCategory.B1, 0.050)]
    [InlineData(LoanRiskCategory.B2, 0.100)]
    [InlineData(LoanRiskCategory.C1, 0.250)]
    [InlineData(LoanRiskCategory.C2, 0.500)]
    [InlineData(LoanRiskCategory.D, 0.750)]
    [InlineData(LoanRiskCategory.E, 1.000)]
    public void GetProvisionRate_ReturnsCorrectRate(LoanRiskCategory category, double expectedRate)
    {
        RiskCategoryTable.GetProvisionRate(category).Should().Be((decimal)expectedRate);
    }

    // ── Severity ordering ───────────────────────────────────────────────────

    [Fact]
    public void Severity_IsMonotonicallyIncreasing()
    {
        var categories = new[]
        {
            LoanRiskCategory.A1, LoanRiskCategory.A2,
            LoanRiskCategory.B1, LoanRiskCategory.B2,
            LoanRiskCategory.C1, LoanRiskCategory.C2,
            LoanRiskCategory.D,  LoanRiskCategory.E
        };

        for (var i = 1; i < categories.Length; i++)
        {
            RiskCategoryTable.Severity(categories[i])
                .Should().BeGreaterThan(RiskCategoryTable.Severity(categories[i - 1]));
        }
    }
}
