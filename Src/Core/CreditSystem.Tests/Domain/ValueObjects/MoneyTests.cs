using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.ValueObjects;

public class MoneyTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidAmount_ShouldCreateMoney()
    {
        // Arrange & Act
        var money = new Money(100.50m, "USD");

        // Assert
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_WithDefaultCurrency_ShouldUseUSD()
    {
        // Arrange & Act
        var money = new Money(100m);

        // Assert
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ShouldThrowDomainException()
    {
        // Arrange & Act
        var act = () => new Money(-100m, "USD");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Money cannot be negative");
    }

    [Theory]
    [InlineData(100.555, 100.56)]  // Rounds up
    [InlineData(100.554, 100.55)]  // Rounds down
    [InlineData(100.5, 100.5)]     // No rounding needed
    public void Constructor_ShouldRoundToTwoDecimals(decimal input, decimal expected)
    {
        // Arrange & Act
        var money = new Money(input, "USD");

        // Assert
        money.Amount.Should().Be(expected);
    }

    #endregion

    #region Zero Factory Tests

    [Fact]
    public void Zero_ShouldCreateMoneyWithZeroAmount()
    {
        // Arrange & Act
        var money = Money.Zero("EUR");

        // Assert
        money.Amount.Should().Be(0);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Zero_WithDefaultCurrency_ShouldUseUSD()
    {
        // Arrange & Act
        var money = Money.Zero();

        // Assert
        money.Currency.Should().Be("USD");
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_WithSameCurrency_ShouldReturnCorrectSum()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50.75m, "USD");

        // Act
        var result = money1.Add(money2);

        // Assert
        result.Amount.Should().Be(150.75m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_WithDifferentCurrency_ShouldThrowDomainException()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50m, "EUR");

        // Act
        var act = () => money1.Add(money2);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Currency mismatch: USD vs EUR");
    }

    [Fact]
    public void AddOperator_ShouldWork()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50m, "USD");

        // Act
        var result = money1 + money2;

        // Assert
        result.Amount.Should().Be(150m);
    }

    #endregion

    #region Subtract Tests

    [Fact]
    public void Subtract_WithSameCurrency_ShouldReturnCorrectDifference()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(30.25m, "USD");

        // Act
        var result = money1.Subtract(money2);

        // Assert
        result.Amount.Should().Be(69.75m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Subtract_WithInsufficientFunds_ShouldThrowDomainException()
    {
        // Arrange
        var money1 = new Money(50m, "USD");
        var money2 = new Money(100m, "USD");

        // Act
        var act = () => money1.Subtract(money2);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Insufficient funds");
    }

    [Fact]
    public void Subtract_WithDifferentCurrency_ShouldThrowDomainException()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50m, "EUR");

        // Act
        var act = () => money1.Subtract(money2);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Currency mismatch: USD vs EUR");
    }

    [Fact]
    public void SubtractOperator_ShouldWork()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(30m, "USD");

        // Act
        var result = money1 - money2;

        // Assert
        result.Amount.Should().Be(70m);
    }

    #endregion

    #region Percentage Tests

    [Theory]
    [InlineData(100, 10, 10)]      // 10% of 100 = 10
    [InlineData(200, 5.5, 11)]     // 5.5% of 200 = 11
    [InlineData(1000, 18, 180)]    // 18% of 1000 = 180
    [InlineData(100, 0, 0)]        // 0% of anything = 0
    public void Percentage_ShouldCalculateCorrectly(decimal amount, decimal percent, decimal expected)
    {
        // Arrange
        var money = new Money(amount, "USD");

        // Act
        var result = money.Percentage(percent);

        // Assert
        result.Amount.Should().Be(expected);
        result.Currency.Should().Be("USD");
    }

    #endregion

    #region Comparison Operators Tests

    [Fact]
    public void GreaterThanOperator_ShouldReturnCorrectResult()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50m, "USD");
        var money3 = new Money(100m, "USD");

        // Assert
        (money1 > money2).Should().BeTrue();
        (money2 > money1).Should().BeFalse();
        (money1 > money3).Should().BeFalse();
    }

    [Fact]
    public void LessThanOperator_ShouldReturnCorrectResult()
    {
        // Arrange
        var money1 = new Money(50m, "USD");
        var money2 = new Money(100m, "USD");
        var money3 = new Money(50m, "USD");

        // Assert
        (money1 < money2).Should().BeTrue();
        (money2 < money1).Should().BeFalse();
        (money1 < money3).Should().BeFalse();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "USD");

        // Assert
        money1.Should().Be(money2);
        (money1 == money2).Should().BeTrue();
    }

    [Fact]
    public void Equality_WithDifferentAmounts_ShouldNotBeEqual()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(50m, "USD");

        // Assert
        money1.Should().NotBe(money2);
        (money1 != money2).Should().BeTrue();
    }

    [Fact]
    public void Equality_WithDifferentCurrencies_ShouldNotBeEqual()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "EUR");

        // Assert
        money1.Should().NotBe(money2);
    }

    #endregion
}
