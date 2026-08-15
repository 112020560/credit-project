using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.Services.Amortization;

public class GermanAmortizationCalculatorTests
{
    private readonly GermanAmortizationCalculator _calculator = new();

    [Fact]
    public void Method_ShouldReturnGerman()
    {
        _calculator.Method.Should().Be(AmortizationMethod.German);
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
    public void Calculate_PrincipalPaymentsShouldBeConstant()
    {
        // Arrange
        var principal = new Money(12000m, "USD");
        var rate = new InterestRate(12m);
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        // All but the last payment should have equal principal
        var principalPayments = schedule.Entries.Take(11).Select(e => e.Principal.Amount);
        principalPayments.Should().AllBeEquivalentTo(1000m); // 12000 / 12
    }

    [Fact]
    public void Calculate_TotalPaymentsShouldDecrease()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m);
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        var totalPayments = schedule.Entries.Select(e => e.TotalPayment.Amount).ToList();
        totalPayments.Should().BeInDescendingOrder();
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
    public void Calculate_FirstPaymentShouldBeHighest()
    {
        // Arrange
        var principal = new Money(12000m, "USD");
        var rate = new InterestRate(12m); // 1% monthly
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        var firstPayment = schedule.Entries.First().TotalPayment.Amount;
        // First payment = 1000 (principal) + 120 (1% of 12000) = 1120
        firstPayment.Should().Be(1120m);
    }

    [Fact]
    public void Calculate_LastPaymentShouldBeLowest()
    {
        // Arrange
        var principal = new Money(12000m, "USD");
        var rate = new InterestRate(12m); // 1% monthly
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = _calculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        var lastPayment = schedule.Entries.Last().TotalPayment.Amount;
        // Last payment = 1000 (principal) + 10 (1% of 1000) = 1010
        lastPayment.Should().Be(1010m);
    }

    [Fact]
    public void Calculate_TotalInterestShouldBeLessThanFrench()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m);
        var termMonths = 12;
        var startDate = DateTime.Today;
        var frenchCalculator = new FrenchAmortizationCalculator();

        // Act
        var germanSchedule = _calculator.Calculate(principal, rate, termMonths, startDate);
        var frenchSchedule = frenchCalculator.Calculate(principal, rate, termMonths, startDate);

        // Assert
        germanSchedule.TotalInterest.Amount.Should().BeLessThan(frenchSchedule.TotalInterest.Amount);
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
}
