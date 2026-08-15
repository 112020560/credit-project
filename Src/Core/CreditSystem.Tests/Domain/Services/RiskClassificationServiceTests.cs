using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Services;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.Services;

public class RiskClassificationServiceTests
{
    private readonly RiskClassificationService _sut = new();

    [Fact]
    public void Classify_A1_ProvisionIsZero()
    {
        var (category, provision) = _sut.Classify(0, 50_000m);

        category.Should().Be(LoanRiskCategory.A1);
        provision.Should().Be(0m);
    }

    [Fact]
    public void Classify_A2_Provision0_5Percent()
    {
        var balance = 10_000m;
        var (category, provision) = _sut.Classify(15, balance);

        category.Should().Be(LoanRiskCategory.A2);
        provision.Should().Be(balance * 0.005m);
    }

    [Fact]
    public void Classify_B1_Provision5Percent()
    {
        var balance = 10_000m;
        var (category, provision) = _sut.Classify(45, balance);

        category.Should().Be(LoanRiskCategory.B1);
        provision.Should().Be(balance * 0.05m);
    }

    [Fact]
    public void Classify_B2_Provision10Percent()
    {
        var balance = 10_000m;
        var (category, provision) = _sut.Classify(75, balance);

        category.Should().Be(LoanRiskCategory.B2);
        provision.Should().Be(balance * 0.10m);
    }

    [Fact]
    public void Classify_C1_Provision25Percent()
    {
        var balance = 10_000m;
        var (category, provision) = _sut.Classify(100, balance);

        category.Should().Be(LoanRiskCategory.C1);
        provision.Should().Be(balance * 0.25m);
    }

    [Fact]
    public void Classify_C2_Provision50Percent()
    {
        var balance = 10_000m;
        var (category, provision) = _sut.Classify(150, balance);

        category.Should().Be(LoanRiskCategory.C2);
        provision.Should().Be(balance * 0.50m);
    }

    [Fact]
    public void Classify_D_Provision75Percent()
    {
        var balance = 10_000m;
        var (category, provision) = _sut.Classify(250, balance);

        category.Should().Be(LoanRiskCategory.D);
        provision.Should().Be(balance * 0.75m);
    }

    [Fact]
    public void Classify_E_ProvisionEqualsFullBalance()
    {
        var balance = 8_500m;
        var (category, provision) = _sut.Classify(400, balance);

        category.Should().Be(LoanRiskCategory.E);
        provision.Should().Be(balance); // 100% provision
    }

    [Fact]
    public void Classify_ZeroBalance_ProvisionIsAlwaysZero()
    {
        var (_, provision) = _sut.Classify(200, 0m);

        provision.Should().Be(0m);
    }
}
