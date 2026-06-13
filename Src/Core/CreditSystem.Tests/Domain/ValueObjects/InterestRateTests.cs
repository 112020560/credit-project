using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.ValueObjects;

public class InterestRateTests
{
    #region Constructor Tests

    [Theory]
    [InlineData(0)]
    [InlineData(5.5)]
    [InlineData(18)]
    [InlineData(100)]
    public void Constructor_WithValidRate_ShouldCreateInterestRate(decimal rate)
    {
        // Arrange & Act
        var interestRate = new InterestRate(rate);

        // Assert
        interestRate.AnnualRate.Should().Be(rate);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    [InlineData(150)]
    public void Constructor_WithInvalidRate_ShouldThrowDomainException(decimal rate)
    {
        // Arrange & Act
        var act = () => new InterestRate(rate);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Invalid interest rate");
    }

    #endregion

    #region MonthlyRate Tests

    [Theory]
    [InlineData(12, 0.01)]        // 12% annual = 1% monthly = 0.01
    [InlineData(24, 0.02)]        // 24% annual = 2% monthly = 0.02
    [InlineData(18, 0.015)]       // 18% annual = 1.5% monthly = 0.015
    [InlineData(0, 0)]            // 0% annual = 0% monthly
    public void MonthlyRate_ShouldCalculateCorrectly(decimal annualRate, decimal expectedMonthlyRate)
    {
        // Arrange
        var interestRate = new InterestRate(annualRate);

        // Act
        var monthlyRate = interestRate.MonthlyRate;

        // Assert
        monthlyRate.Should().Be(expectedMonthlyRate);
    }

    #endregion

    #region DailyRate Tests

    [Fact]
    public void DailyRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var interestRate = new InterestRate(36.5m); // 36.5% annual = 0.1% daily

        // Act
        var dailyRate = interestRate.DailyRate;

        // Assert
        dailyRate.Should().Be(0.001m);
    }

    [Fact]
    public void DailyRate_WithZeroRate_ShouldBeZero()
    {
        // Arrange
        var interestRate = new InterestRate(0);

        // Act
        var dailyRate = interestRate.DailyRate;

        // Assert
        dailyRate.Should().Be(0);
    }

    #endregion

    #region CalculateMonthlyInterest Tests

    [Fact]
    public void CalculateMonthlyInterest_ShouldReturnCorrectAmount()
    {
        // Arrange
        var interestRate = new InterestRate(12m); // 12% annual = 1% monthly
        var principal = new Money(10000m, "USD");

        // Act
        var monthlyInterest = interestRate.CalculateMonthlyInterest(principal);

        // Assert
        monthlyInterest.Amount.Should().Be(100m); // 1% of 10000 = 100
        monthlyInterest.Currency.Should().Be("USD");
    }

    [Fact]
    public void CalculateMonthlyInterest_WithZeroPrincipal_ShouldReturnZero()
    {
        // Arrange
        var interestRate = new InterestRate(18m);
        var principal = Money.Zero("USD");

        // Act
        var monthlyInterest = interestRate.CalculateMonthlyInterest(principal);

        // Assert
        monthlyInterest.Amount.Should().Be(0);
    }

    [Fact]
    public void CalculateMonthlyInterest_WithZeroRate_ShouldReturnZero()
    {
        // Arrange
        var interestRate = new InterestRate(0);
        var principal = new Money(10000m, "USD");

        // Act
        var monthlyInterest = interestRate.CalculateMonthlyInterest(principal);

        // Assert
        monthlyInterest.Amount.Should().Be(0);
    }

    [Fact]
    public void CalculateMonthlyInterest_ShouldPreserveCurrency()
    {
        // Arrange
        var interestRate = new InterestRate(12m);
        var principal = new Money(1000m, "EUR");

        // Act
        var monthlyInterest = interestRate.CalculateMonthlyInterest(principal);

        // Assert
        monthlyInterest.Currency.Should().Be("EUR");
    }

    #endregion

    #region CalculateDailyInterest Tests

    [Fact]
    public void CalculateDailyInterest_ShouldReturnCorrectAmount()
    {
        // Arrange
        var interestRate = new InterestRate(36.5m); // 36.5% annual = 0.1% daily
        var principal = new Money(10000m, "USD");

        // Act
        var dailyInterest = interestRate.CalculateDailyInterest(principal);

        // Assert
        dailyInterest.Amount.Should().Be(10m); // 0.1% of 10000 = 10
        dailyInterest.Currency.Should().Be("USD");
    }

    [Fact]
    public void CalculateDailyInterest_WithZeroPrincipal_ShouldReturnZero()
    {
        // Arrange
        var interestRate = new InterestRate(18m);
        var principal = Money.Zero("USD");

        // Act
        var dailyInterest = interestRate.CalculateDailyInterest(principal);

        // Assert
        dailyInterest.Amount.Should().Be(0);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_WithSameRate_ShouldBeEqual()
    {
        // Arrange
        var rate1 = new InterestRate(18m);
        var rate2 = new InterestRate(18m);

        // Assert
        rate1.Should().Be(rate2);
        (rate1 == rate2).Should().BeTrue();
    }

    [Fact]
    public void Equality_WithDifferentRates_ShouldNotBeEqual()
    {
        // Arrange
        var rate1 = new InterestRate(18m);
        var rate2 = new InterestRate(12m);

        // Assert
        rate1.Should().NotBe(rate2);
        (rate1 != rate2).Should().BeTrue();
    }

    #endregion
}
