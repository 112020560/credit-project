using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.Entities;

public class LoanGuaranteeTests
{
    private static GuaranteeValuation DefaultValuation() => new(10_000_000m, 0.80m);

    [Fact]
    public void Constructor_WithValidData_ShouldCreateWithVigenteStatus()
    {
        var guarantee = new LoanGuarantee(
            Guid.NewGuid(),
            Guid.NewGuid(),
            GuaranteeType.Hipoteca,
            "Casa en San José",
            DefaultValuation());

        guarantee.Status.Should().Be(GuaranteeStatus.Vigente);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithEmptyDescription_ShouldThrow(string? description)
    {
        var act = () => new LoanGuarantee(
            Guid.NewGuid(),
            Guid.NewGuid(),
            GuaranteeType.Hipoteca,
            description!,
            DefaultValuation());

        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }

    [Fact]
    public void Constructor_WithNullValuation_ShouldThrow()
    {
        var act = () => new LoanGuarantee(
            Guid.NewGuid(),
            Guid.NewGuid(),
            GuaranteeType.Prenda,
            "Vehículo",
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("valuation");
    }
}
