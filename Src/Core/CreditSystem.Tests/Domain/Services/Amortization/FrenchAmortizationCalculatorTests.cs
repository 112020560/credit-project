using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.Services.Amortization;

public class FrenchAmortizationCalculatorTests
{
    private readonly FrenchAmortizationCalculator _calculator = new();

    [Fact]
    public void Method_ShouldReturnFrench()
    {
        _calculator.Method.Should().Be(AmortizationMethod.French);
    }

    [Fact]
    public void Calculate_ShouldGenerateCorrectNumberOfPayments()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m);
        var termMonths = 12;
        var startDate = new DateTime(2024, 1, 1);

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        schedule.Entries.Should().HaveCount(12);
    }

    [Fact]
    public void Calculate_AllPaymentsExceptLastShouldBeEqual()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m);
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        // All payments except the last should be equal (last may differ due to rounding adjustment)
        var firstPayment = schedule.Entries.First().TotalPayment.Amount;
        schedule.Entries.Take(termMonths - 1).Should().AllSatisfy(e =>
            e.TotalPayment.Amount.Should().Be(firstPayment));
    }

    [Fact]
    public void Calculate_InterestShouldDecreaseOverTime()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m);
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        var interests = schedule.Entries.Select(e => e.Interest.Amount).ToList();
        interests.Should().BeInDescendingOrder();
    }

    [Fact]
    public void Calculate_PrincipalShouldGenerallyIncreaseOverTime()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m);
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        // Principal payments should generally increase (last may be adjusted for rounding)
        var principals = schedule.Entries.Take(termMonths - 1).Select(e => e.Principal.Amount).ToList();
        principals.Should().BeInAscendingOrder();
    }

    [Fact]
    public void Calculate_FinalBalanceShouldBeZero()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(18m);
        var termMonths = 24;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        schedule.Entries.Last().Balance.Amount.Should().Be(0);
    }

    [Fact]
    public void Calculate_TotalPrincipalPaidShouldEqualOriginalPrincipal()
    {
        // Arrange
        var principal = new Money(15000m, "USD");
        var rate = new InterestRate(24m);
        var termMonths = 36;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        var totalPrincipalPaid = schedule.Entries.Sum(e => e.Principal.Amount);
        totalPrincipalPaid.Should().BeApproximately(principal.Amount, 0.01m);
    }

    [Fact]
    public void Calculate_DueDatesShouldBeMonthlyIncremented()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m);
        var termMonths = 6;
        var startDate = new DateTime(2024, 1, 15);

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        schedule.Entries[0].DueDate.Should().Be(new DateTime(2024, 2, 15));
        schedule.Entries[1].DueDate.Should().Be(new DateTime(2024, 3, 15));
        schedule.Entries[2].DueDate.Should().Be(new DateTime(2024, 4, 15));
        schedule.Entries[3].DueDate.Should().Be(new DateTime(2024, 5, 15));
        schedule.Entries[4].DueDate.Should().Be(new DateTime(2024, 6, 15));
        schedule.Entries[5].DueDate.Should().Be(new DateTime(2024, 7, 15));
    }

    [Fact]
    public void Calculate_WithZeroInterestRate_ShouldDividePrincipalEqually()
    {
        // Arrange
        var principal = new Money(12000m, "USD");
        var rate = new InterestRate(0);
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        schedule.Entries.Should().AllSatisfy(e =>
        {
            e.TotalPayment.Amount.Should().Be(1000m); // 12000 / 12
            e.Principal.Amount.Should().Be(1000m);
            e.Interest.Amount.Should().Be(0);
        });
    }

    [Fact]
    public void Calculate_ShouldPreserveCurrency()
    {
        // Arrange
        var principal = new Money(10000m, "EUR");
        var rate = new InterestRate(12m);
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        schedule.Entries.Should().AllSatisfy(e =>
        {
            e.TotalPayment.Currency.Should().Be("EUR");
            e.Principal.Currency.Should().Be("EUR");
            e.Interest.Currency.Should().Be("EUR");
            e.Balance.Currency.Should().Be("EUR");
        });
    }

    [Theory]
    [InlineData(10000, 12, 12, 888.49)]   // Known French amortization result
    [InlineData(10000, 24, 12, 945.60)]   // Higher rate (calculated by system)
    [InlineData(10000, 12, 24, 470.73)]   // Longer term
    public void Calculate_ShouldMatchExpectedMonthlyPayment(
        decimal principalAmount,
        decimal annualRate,
        int termMonths,
        decimal expectedPayment)
    {
        // Arrange
        var principal = new Money(principalAmount, "USD");
        var rate = new InterestRate(annualRate);
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        // Allow 0.05 tolerance for rounding differences across implementations
        schedule.Entries.First().TotalPayment.Amount.Should().BeApproximately(expectedPayment, 0.05m);
    }
}
