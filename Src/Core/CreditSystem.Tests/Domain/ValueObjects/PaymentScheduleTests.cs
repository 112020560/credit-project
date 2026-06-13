using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.ValueObjects;

public class PaymentScheduleTests
{
    [Fact]
    public void Constructor_WithEntries_ShouldCalculateTotals()
    {
        // Arrange
        var entries = new List<AmortizationEntry>
        {
            new AmortizationEntry(1, DateTime.Today.AddMonths(1),
                new Money(500m), new Money(400m), new Money(100m), new Money(9600m)),
            new AmortizationEntry(2, DateTime.Today.AddMonths(2),
                new Money(500m), new Money(410m), new Money(90m), new Money(9190m)),
            new AmortizationEntry(3, DateTime.Today.AddMonths(3),
                new Money(500m), new Money(420m), new Money(80m), new Money(8770m))
        };

        // Act
        var schedule = new PaymentSchedule(entries, AmortizationMethod.French);

        // Assert
        schedule.Entries.Should().HaveCount(3);
        schedule.TotalInterest.Amount.Should().Be(270m); // 100 + 90 + 80
        schedule.TotalPayment.Amount.Should().Be(1500m); // 500 * 3
        schedule.Method.Should().Be(AmortizationMethod.French);
    }

    [Fact]
    public void Constructor_WithEmptyEntries_ShouldHaveZeroTotals()
    {
        // Arrange & Act
        var schedule = new PaymentSchedule(Enumerable.Empty<AmortizationEntry>());

        // Assert
        schedule.Entries.Should().BeEmpty();
        schedule.TotalInterest.Amount.Should().Be(0);
        schedule.TotalPayment.Amount.Should().Be(0);
    }

    [Fact]
    public void GetCurrentPayment_ShouldReturnFirstUnpaidEntry()
    {
        // Arrange
        var today = DateTime.Today;
        var entries = new List<AmortizationEntry>
        {
            new AmortizationEntry(1, today.AddDays(-10),
                new Money(500m), new Money(400m), new Money(100m), new Money(0m)), // Paid (balance = 0)
            new AmortizationEntry(2, today.AddDays(5),
                new Money(500m), new Money(410m), new Money(90m), new Money(9190m)), // Due soon
            new AmortizationEntry(3, today.AddDays(35),
                new Money(500m), new Money(420m), new Money(80m), new Money(8770m))
        };
        var schedule = new PaymentSchedule(entries);

        // Act
        var currentPayment = schedule.GetCurrentPayment(today);

        // Assert
        currentPayment.Should().NotBeNull();
        currentPayment!.PaymentNumber.Should().Be(2);
    }

    [Fact]
    public void GetCurrentPayment_WhenAllPaid_ShouldReturnNull()
    {
        // Arrange
        var today = DateTime.Today;
        var entries = new List<AmortizationEntry>
        {
            new AmortizationEntry(1, today.AddDays(-30),
                new Money(500m), new Money(500m), new Money(0m), new Money(0m)),
            new AmortizationEntry(2, today.AddDays(-10),
                new Money(500m), new Money(500m), new Money(0m), new Money(0m))
        };
        var schedule = new PaymentSchedule(entries);

        // Act
        var currentPayment = schedule.GetCurrentPayment(today);

        // Assert
        currentPayment.Should().BeNull();
    }

    [Fact]
    public void Calculate_ShouldGenerateCorrectSchedule()
    {
        // Arrange
        var principal = new Money(12000m, "USD");
        var rate = new InterestRate(12m); // 12% annual = 1% monthly
        var termMonths = 12;
        var startDate = new DateTime(2024, 1, 1);

        // Act
        var schedule = PaymentSchedule.Calculate(principal, rate, termMonths, startDate);

        // Assert
        schedule.Entries.Should().HaveCount(12);
        schedule.Entries.First().PaymentNumber.Should().Be(1);
        schedule.Entries.Last().PaymentNumber.Should().Be(12);
        schedule.Entries.Last().Balance.Amount.Should().Be(0);
    }

    [Fact]
    public void Calculate_AllPaymentsExceptLastShouldHaveSameAmount()
    {
        // Arrange (French amortization has fixed payments, except last may vary due to rounding)
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(12m);
        var termMonths = 12;
        var startDate = DateTime.Today;

        // Act
        var schedule = PaymentSchedule.Calculate(principal, rate, termMonths, startDate);

        // Assert
        var paymentsExceptLast = schedule.Entries.Take(termMonths - 1).Select(e => e.TotalPayment.Amount).ToList();
        paymentsExceptLast.Should().AllBeEquivalentTo(paymentsExceptLast.First());
    }

    [Fact]
    public void Calculate_TotalPrincipalPaidShouldEqualOriginalPrincipal()
    {
        // Arrange
        var principal = new Money(10000m, "USD");
        var rate = new InterestRate(18m);
        var termMonths = 24;
        var startDate = DateTime.Today;

        // Act
        var schedule = PaymentSchedule.Calculate(principal, rate, termMonths, startDate);

        // Assert
        var totalPrincipalPaid = schedule.Entries.Sum(e => e.Principal.Amount);
        totalPrincipalPaid.Should().BeApproximately(principal.Amount, 0.01m);
    }
}
