using CreditSystem.Domain.Aggregates.LoanContract;
using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.Aggregates;

public class LoanContractAggregateTests
{
    private readonly FrenchAmortizationCalculator _calculator = new();

    private LoanContractAggregate CreateValidContract(
        decimal principal = 10000m,
        decimal rate = 12m,
        int termMonths = 12)
    {
        return LoanContractAggregate.Create(
            customerId: Guid.NewGuid(),
            principal: new Money(principal, "USD"),
            rate: new InterestRate(rate),
            termMonths: termMonths,
            amortizationMethod: AmortizationMethod.French,
            calculator: _calculator,
            evaluationMetadata: new Dictionary<string, object>());
    }

    #region Create Tests

    [Fact]
    public void Create_ShouldInitializeWithApprovedStatus()
    {
        // Act
        var contract = CreateValidContract();

        // Assert
        contract.State.Status.Should().Be(ContractStatus.Approved);
    }

    [Fact]
    public void Create_ShouldSetCorrectPrincipal()
    {
        // Arrange
        var principal = 15000m;

        // Act
        var contract = CreateValidContract(principal: principal);

        // Assert
        contract.State.Principal.Amount.Should().Be(principal);
        contract.State.CurrentBalance.Amount.Should().Be(principal);
    }

    [Fact]
    public void Create_ShouldGeneratePaymentSchedule()
    {
        // Arrange
        var termMonths = 12;

        // Act
        var contract = CreateValidContract(termMonths: termMonths);

        // Assert
        contract.State.Schedule.Should().NotBeNull();
        contract.State.Schedule.Entries.Should().HaveCount(termMonths);
    }

    [Fact]
    public void Create_ShouldGenerateContractCreatedEvent()
    {
        // Act
        var contract = CreateValidContract();

        // Assert
        contract.UncommittedEvents.Should().HaveCount(1);
        contract.UncommittedEvents.First().Should().BeOfType<ContractCreated>();
    }

    [Fact]
    public void Create_ShouldSetNextPaymentDue()
    {
        // Act
        var contract = CreateValidContract();

        // Assert
        contract.State.NextPaymentDue.Should().NotBeNull();
        contract.State.NextPaymentDue.Should().BeAfter(DateTime.UtcNow);
    }

    #endregion

    #region Disburse Tests

    [Fact]
    public void Disburse_WhenApproved_ShouldChangeStatusToActive()
    {
        // Arrange
        var contract = CreateValidContract();

        // Act
        contract.Disburse("WIRE", "1234567890");

        // Assert
        contract.State.Status.Should().Be(ContractStatus.Active);
        contract.State.DisbursedAt.Should().NotBeNull();
    }

    [Fact]
    public void Disburse_ShouldGenerateLoanDisbursedEvent()
    {
        // Arrange
        var contract = CreateValidContract();

        // Act
        contract.Disburse("ACH", "0987654321");

        // Assert
        contract.UncommittedEvents.Should().HaveCount(2);
        contract.UncommittedEvents.Last().Should().BeOfType<LoanDisbursed>();
    }

    [Fact]
    public void Disburse_WhenAlreadyDisbursed_ShouldThrow()
    {
        // Arrange
        var contract = CreateValidContract();
        contract.Disburse("WIRE", "123");

        // Act
        var act = () => contract.Disburse("WIRE", "456");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Cannot disburse*");
    }

    #endregion

    #region AccrueInterest Tests

    [Fact]
    public void AccrueInterest_WhenActive_ShouldIncreaseAccruedInterest()
    {
        // Arrange
        var contract = CreateValidContract(principal: 10000m, rate: 36.5m); // 0.1% daily
        contract.Disburse("WIRE", "123");
        var start = DateTime.UtcNow.AddDays(-10);
        var end = DateTime.UtcNow;

        // Act
        contract.AccrueInterest(start, end);

        // Assert
        contract.State.AccruedInterest.Amount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void AccrueInterest_WhenNotActive_ShouldThrow()
    {
        // Arrange
        var contract = CreateValidContract();
        // Not disbursed, still Approved

        // Act
        var act = () => contract.AccrueInterest(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Cannot accrue interest*");
    }

    #endregion

    #region ApplyPayment Tests

    [Fact]
    public void ApplyPayment_ShouldReduceBalance()
    {
        // Arrange
        var contract = CreateValidContract(principal: 10000m);
        contract.Disburse("WIRE", "123");
        var initialBalance = contract.State.CurrentBalance.Amount;
        var paymentAmount = 1000m;

        // Act
        contract.ApplyPayment(Guid.NewGuid(), new Money(paymentAmount, "USD"), PaymentMethod.Wire);

        // Assert
        contract.State.CurrentBalance.Amount.Should().Be(initialBalance - paymentAmount);
    }

    [Fact]
    public void ApplyPayment_ShouldIncrementPaymentsMade()
    {
        // Arrange
        var contract = CreateValidContract();
        contract.Disburse("WIRE", "123");

        // Act
        contract.ApplyPayment(Guid.NewGuid(), new Money(500m, "USD"), PaymentMethod.Wire);

        // Assert
        contract.State.PaymentsMade.Should().Be(1);
    }

    [Fact]
    public void ApplyPayment_ShouldPayFeesFirst()
    {
        // Arrange
        var contract = CreateValidContract(principal: 10000m);
        contract.Disburse("WIRE", "123");
        // Simulate a missed payment to generate fees
        contract.RecordMissedPayment(1, DateTime.UtcNow.AddDays(-35), new Money(50m, "USD"));

        var feesBeforePayment = contract.State.TotalFees.Amount;

        // Act
        contract.ApplyPayment(Guid.NewGuid(), new Money(50m, "USD"), PaymentMethod.Wire);

        // Assert
        contract.State.TotalFees.Amount.Should().BeLessThan(feesBeforePayment);
    }

    [Fact]
    public void ApplyPayment_WhenFullyPaid_ShouldMarkAsPaidOff()
    {
        // Arrange
        var contract = CreateValidContract(principal: 1000m, rate: 0, termMonths: 1);
        contract.Disburse("WIRE", "123");

        // Act
        contract.ApplyPayment(Guid.NewGuid(), new Money(1000m, "USD"), PaymentMethod.Wire);

        // Assert
        contract.State.Status.Should().Be(ContractStatus.PaidOff);
        contract.State.CurrentBalance.Amount.Should().Be(0);
        contract.UncommittedEvents.OfType<ContractPaidOff>().Should().HaveCount(1);
    }

    [Fact]
    public void ApplyPayment_WithWrongCurrency_ShouldThrow()
    {
        // Arrange
        var contract = CreateValidContract();
        contract.Disburse("WIRE", "123");

        // Act
        var act = () => contract.ApplyPayment(
            Guid.NewGuid(),
            new Money(500m, "EUR"),
            PaymentMethod.Wire);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Currency mismatch*");
    }

    [Fact]
    public void ApplyPayment_WhenNotDisbursed_ShouldThrow()
    {
        // Arrange
        var contract = CreateValidContract();

        // Act
        var act = () => contract.ApplyPayment(
            Guid.NewGuid(),
            new Money(500m, "USD"),
            PaymentMethod.Wire);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Contract not in payable status");
    }

    #endregion

    #region RecordMissedPayment Tests

    [Fact]
    public void RecordMissedPayment_ShouldIncrementPaymentsMissed()
    {
        // Arrange
        var contract = CreateValidContract();
        contract.Disburse("WIRE", "123");

        // Act
        contract.RecordMissedPayment(1, DateTime.UtcNow.AddDays(-35), new Money(25m, "USD"));

        // Assert
        contract.State.PaymentsMissed.Should().Be(1);
    }

    [Fact]
    public void RecordMissedPayment_ShouldChangeStatusToDelinquent()
    {
        // Arrange
        var contract = CreateValidContract();
        contract.Disburse("WIRE", "123");

        // Act
        contract.RecordMissedPayment(1, DateTime.UtcNow.AddDays(-35), new Money(25m, "USD"));

        // Assert
        contract.State.Status.Should().Be(ContractStatus.Delinquent);
    }

    [Fact]
    public void RecordMissedPayment_ShouldAddLateFee()
    {
        // Arrange
        var contract = CreateValidContract();
        contract.Disburse("WIRE", "123");
        var lateFee = 50m;

        // Act
        contract.RecordMissedPayment(1, DateTime.UtcNow.AddDays(-35), new Money(lateFee, "USD"));

        // Assert
        contract.State.TotalFees.Amount.Should().Be(lateFee);
    }

    [Fact]
    public void RecordMissedPayment_After90Days_ShouldAutoDefault()
    {
        // Arrange
        var contract = CreateValidContract();
        contract.Disburse("WIRE", "123");

        // Act
        contract.RecordMissedPayment(1, DateTime.UtcNow.AddDays(-91), new Money(25m, "USD"));

        // Assert
        contract.State.Status.Should().Be(ContractStatus.Default);
    }

    #endregion

    #region MarkAsDefault Tests

    [Fact]
    public void MarkAsDefault_ShouldChangeStatusToDefault()
    {
        // Arrange
        var contract = CreateValidContract();
        contract.Disburse("WIRE", "123");

        // Act
        contract.MarkAsDefault("Customer bankruptcy");

        // Assert
        contract.State.Status.Should().Be(ContractStatus.Default);
        contract.State.DefaultedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsDefault_WhenAlreadyDefault_ShouldNotAddEvent()
    {
        // Arrange
        var contract = CreateValidContract();
        contract.Disburse("WIRE", "123");
        contract.MarkAsDefault("First default");
        var eventCountAfterFirst = contract.UncommittedEvents.Count;

        // Act
        contract.MarkAsDefault("Second default attempt");

        // Assert
        contract.UncommittedEvents.Count.Should().Be(eventCountAfterFirst);
    }

    #endregion

    #region Restructure Tests

    [Fact]
    public void Restructure_WhenDelinquent_ShouldCreateNewSchedule()
    {
        // Arrange
        var contract = CreateValidContract(principal: 10000m, rate: 18m, termMonths: 12);
        contract.Disburse("WIRE", "123");
        contract.RecordMissedPayment(1, DateTime.UtcNow.AddDays(-35), new Money(25m, "USD"));
        // Now it's Delinquent

        // Act
        contract.Restructure(
            newRate: new InterestRate(12m),
            newTermMonths: 24,
            forgiveAmount: new Money(1000m, "USD"),
            reason: "Customer hardship");

        // Assert
        contract.State.InterestRate.AnnualRate.Should().Be(12m);
        contract.State.TermMonths.Should().Be(24);
        contract.State.Schedule.Entries.Should().HaveCount(24);
    }

    [Fact]
    public void Restructure_ShouldReduceBalanceByForgiveAmount()
    {
        // Arrange
        var contract = CreateValidContract(principal: 10000m);
        contract.Disburse("WIRE", "123");
        contract.RecordMissedPayment(1, DateTime.UtcNow.AddDays(-35), new Money(25m, "USD"));
        var balanceBefore = contract.State.CurrentBalance.Amount;
        var forgiveAmount = 2000m;

        // Act
        contract.Restructure(
            newRate: new InterestRate(12m),
            newTermMonths: 12,
            forgiveAmount: new Money(forgiveAmount, "USD"),
            reason: "Debt relief");

        // Assert
        contract.State.CurrentBalance.Amount.Should().Be(balanceBefore - forgiveAmount);
    }

    [Fact]
    public void Restructure_ShouldResetPaymentsMissedAndActivate()
    {
        // Arrange
        var contract = CreateValidContract();
        contract.Disburse("WIRE", "123");
        contract.RecordMissedPayment(1, DateTime.UtcNow.AddDays(-35), new Money(25m, "USD"));
        contract.State.Status.Should().Be(ContractStatus.Delinquent);

        // Act
        contract.Restructure(
            newRate: new InterestRate(10m),
            newTermMonths: 12,
            forgiveAmount: Money.Zero("USD"),
            reason: "Payment plan");

        // Assert
        contract.State.Status.Should().Be(ContractStatus.Active);
        contract.State.PaymentsMissed.Should().Be(0);
    }

    [Fact]
    public void Restructure_WhenActive_ShouldThrow()
    {
        // Arrange
        var contract = CreateValidContract();
        contract.Disburse("WIRE", "123");
        // Status is Active

        // Act
        var act = () => contract.Restructure(
            newRate: new InterestRate(10m),
            newTermMonths: 12,
            forgiveAmount: Money.Zero("USD"),
            reason: "Test");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*only restructure delinquent or defaulted*");
    }

    #endregion

    #region Event Sourcing Tests

    [Fact]
    public void Rehydrate_ShouldRestoreStateFromEvents()
    {
        // Arrange
        var originalContract = CreateValidContract(principal: 10000m);
        originalContract.Disburse("WIRE", "123");
        originalContract.ApplyPayment(Guid.NewGuid(), new Money(500m, "USD"), PaymentMethod.Wire);

        var events = originalContract.UncommittedEvents.ToList();

        // Act
        var rehydratedContract = new LoanContractAggregate(events);

        // Assert
        rehydratedContract.Id.Should().Be(originalContract.Id);
        rehydratedContract.State.Status.Should().Be(originalContract.State.Status);
        rehydratedContract.State.CurrentBalance.Amount.Should().Be(originalContract.State.CurrentBalance.Amount);
        rehydratedContract.State.PaymentsMade.Should().Be(originalContract.State.PaymentsMade);
    }

    [Fact]
    public void ClearUncommittedEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var contract = CreateValidContract();
        contract.Disburse("WIRE", "123");
        contract.UncommittedEvents.Should().HaveCountGreaterThan(1);

        // Act
        contract.ClearUncommittedEvents();

        // Assert
        contract.UncommittedEvents.Should().BeEmpty();
    }

    #endregion
}
