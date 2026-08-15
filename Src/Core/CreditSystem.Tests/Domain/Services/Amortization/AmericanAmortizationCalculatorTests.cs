using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.Services.Amortization;

public class AmericanAmortizationCalculatorTests
{
    private readonly AmericanAmortizationCalculator _calculator = new();

    [Fact]
    public void Method_ShouldReturnAmerican()
    {
        _calculator.Method.Should().Be(AmortizationMethod.American);
    }

    [Fact]
    public void Calculate_ShouldGenerateCorrectNumberOfPayments()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m);
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        schedule.Entries.Should().HaveCount(12);
    }

    [Fact]
    public void Calculate_OnlyLastPaymentShouldIncludePrincipal()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m);
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        // All payments except the last should have zero principal
        schedule.Entries.Take(11).Should().AllSatisfy(e =>
            e.Principal.Amount.Should().Be(0));

        // Last payment should include full principal
        schedule.Entries.Last().Principal.Amount.Should().Be(principal.Amount);
    }

    [Fact]
    public void Calculate_AllInterestPaymentsShouldBeEqual()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m); // 1% monthly
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        var expectedMonthlyInterest = 100m; // 1% of 10000
        schedule.Entries.Should().AllSatisfy(e =>
            e.Interest.Amount.Should().Be(expectedMonthlyInterest));
    }

    [Fact]
    public void Calculate_RegularPaymentsShouldBeInterestOnly()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m); // 1% monthly = 100 interest
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        // Payments 1-11 should be interest only (100)
        schedule.Entries.Take(11).Should().AllSatisfy(e =>
            e.TotalPayment.Amount.Should().Be(100m));
    }

    [Fact]
    public void Calculate_LastPaymentShouldIncludePrincipalPlusInterest()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m); // 1% monthly = 100 interest
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        var lastPayment = schedule.Entries.Last();
        lastPayment.TotalPayment.Amount.Should().Be(10100m); // 10000 principal + 100 interest
    }

    [Fact]
    public void Calculate_BalanceShouldRemainConstantUntilLastPayment()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m);
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        schedule.Entries.Take(11).Should().AllSatisfy(e =>
            e.Balance.Amount.Should().Be(principal.Amount));

        schedule.Entries.Last().Balance.Amount.Should().Be(0);
    }

    [Fact]
    public void Calculate_TotalPrincipalPaidShouldEqualOriginalPrincipal()
    {
        // Arrange
        var principal = new Money(15000m, "USD");
        var rate = new InterestRate(18m);
        var termMonths = 24;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        var totalPrincipalPaid = schedule.Entries.Sum(e => e.Principal.Amount);
        totalPrincipalPaid.Should().Be(principal.Amount);
    }

    [Fact]
    public void Calculate_TotalInterestShouldBeHigherThanFrench()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m);
        var termMonths = 12;
        var startDate = DateTime.Today;
        var frenchCalculator = new FrenchAmortizationCalculator();

        // Act
        var americanSchedule = _calculator.Calculate(principal, rate, termMonths, startDate);
        var frenchSchedule = frenchCalculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        // American (interest-only) pays more total interest because balance never decreases until the end
        americanSchedule.TotalInterest.Amount.Should().BeGreaterThan(frenchSchedule.TotalInterest.Amount);
    }

    [Fact]
    public void Calculate_TotalInterestShouldEqualMonthlyInterestTimesTermMonths()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m); // 1% monthly = 100
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        schedule.TotalInterest.Amount.Should().Be(1200m); // 100 * 12 months
    }

    [Fact]
    public void Calculate_ShouldPreserveCurrency()
    {
        // Arrange
        var principal = new Money(10000m, "MXN");
        var rate = new InterestRate(12m);
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        schedule.Entries.Should().AllSatisfy(e =>
        {
            e.TotalPayment.Currency.Should().Be("MXN");
            e.Principal.Currency.Should().Be("MXN");
            e.Interest.Currency.Should().Be("MXN");
            e.Balance.Currency.Should().Be("MXN");
        });
    }
}
