using CreditSystem.Domain.Aggregates.RevolvingCredit;
using CreditSystem.Domain.Aggregates.RevolvingCredit.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.Aggregates;

public class RevolvingCreditAggregateTests
{
    private RevolvingCreditAggregate CreateValidCreditLine(
        decimal creditLimit = 10000m,
        decimal rate = 24m,
        int billingCycleDay = 15)
    {
        return RevolvingCreditAggregate.Create(
            customerId: Guid.NewGuid(),
            creditLimit: new Money(creditLimit, "USD"),
            rate: new InterestRate(rate),
            minimumPaymentPercentage: 5m,
            minimumPaymentAmount: new Money(25m, "USD"),
            billingCycleDay: billingCycleDay,
            gracePeriodDays: 21);
    }

    #region Create Tests

    [Fact]
    public void Create_ShouldInitializeWithPendingStatus()
    {
        // Act
        var creditLine = CreateValidCreditLine();

        // Assert
        creditLine.State.Status.Should().Be(RevolvingCreditStatus.Pending);
    }

    [Fact]
    public void Create_ShouldSetCorrectCreditLimit()
    {
        // Arrange
        var limit = 15000m;

        // Act
        var creditLine = CreateValidCreditLine(creditLimit: limit);

        // Assert
        creditLine.State.CreditLimit.Amount.Should().Be(limit);
        creditLine.State.AvailableCredit.Amount.Should().Be(limit);
    }

    [Fact]
    public void Create_ShouldStartWithZeroBalance()
    {
        // Act
        var creditLine = CreateValidCreditLine();

        // Assert
        creditLine.State.CurrentBalance.Amount.Should().Be(0);
    }

    [Fact]
    public void Create_WithInvalidBillingDay_ShouldThrow()
    {
        // Act
        var act = () => RevolvingCreditAggregate.Create(
            customerId: Guid.NewGuid(),
            creditLimit: new Money(10000m, "USD"),
            rate: new InterestRate(24m),
            minimumPaymentPercentage: 5m,
            minimumPaymentAmount: new Money(25m, "USD"),
            billingCycleDay: 31, // Invalid
            gracePeriodDays: 21);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*between 1 and 28*");
    }

    [Fact]
    public void Create_ShouldGenerateCreditLineCreatedEvent()
    {
        // Act
        var creditLine = CreateValidCreditLine();

        // Assert
        creditLine.UncommittedEvents.Should().HaveCount(1);
        creditLine.UncommittedEvents.First().Should().BeOfType<CreditLineCreated>();
    }

    #endregion

    #region Activate Tests

    [Fact]
    public void Activate_WhenPending_ShouldChangeStatusToActive()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();

        // Act
        creditLine.Activate();

        // Assert
        creditLine.State.Status.Should().Be(RevolvingCreditStatus.Active);
        creditLine.State.ActivatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_ShouldSetNextStatementDate()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();

        // Act
        creditLine.Activate();

        // Assert
        creditLine.State.NextStatementDate.Should().NotBeNull();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldThrow()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();

        // Act
        var act = () => creditLine.Activate();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot activate*");
    }

    #endregion

    #region DrawFunds Tests

    [Fact]
    public void DrawFunds_ShouldIncreaseBalance()
    {
        // Arrange
        var creditLine = CreateValidCreditLine(creditLimit: 10000m);
        creditLine.Activate();
        var drawAmount = 3000m;

        // Act
        creditLine.DrawFunds(new Money(drawAmount, "USD"), "Purchase");

        // Assert
        creditLine.State.CurrentBalance.Amount.Should().Be(drawAmount);
    }

    [Fact]
    public void DrawFunds_ShouldDecreaseAvailableCredit()
    {
        // Arrange
        var creditLine = CreateValidCreditLine(creditLimit: 10000m);
        creditLine.Activate();
        var drawAmount = 3000m;

        // Act
        creditLine.DrawFunds(new Money(drawAmount, "USD"), "Purchase");

        // Assert
        creditLine.State.AvailableCredit.Amount.Should().Be(7000m);
    }

    [Fact]
    public void DrawFunds_ExceedingLimit_ShouldThrow()
    {
        // Arrange
        var creditLine = CreateValidCreditLine(creditLimit: 5000m);
        creditLine.Activate();

        // Act
        var act = () => creditLine.DrawFunds(new Money(6000m, "USD"), "Big purchase");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Insufficient credit*");
    }

    [Fact]
    public void DrawFunds_WhenNotActive_ShouldThrow()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        // Not activated

        // Act
        var act = () => creditLine.DrawFunds(new Money(1000m, "USD"), "Purchase");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot draw funds*");
    }

    [Fact]
    public void DrawFunds_ZeroAmount_ShouldThrow()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();

        // Act
        var act = () => creditLine.DrawFunds(new Money(0m, "USD"), "Nothing");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*must be greater than zero*");
    }

    [Fact]
    public void DrawFunds_MultipleTimes_ShouldAccumulate()
    {
        // Arrange
        var creditLine = CreateValidCreditLine(creditLimit: 10000m);
        creditLine.Activate();

        // Act
        creditLine.DrawFunds(new Money(1000m, "USD"), "First");
        creditLine.DrawFunds(new Money(2000m, "USD"), "Second");
        creditLine.DrawFunds(new Money(1500m, "USD"), "Third");

        // Assert
        creditLine.State.CurrentBalance.Amount.Should().Be(4500m);
        creditLine.State.AvailableCredit.Amount.Should().Be(5500m);
    }

    #endregion

    #region ApplyPayment Tests

    [Fact]
    public void ApplyPayment_ShouldReduceBalance()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();
        creditLine.DrawFunds(new Money(5000m, "USD"), "Purchase");

        // Act
        creditLine.ApplyPayment(Guid.NewGuid(), new Money(2000m, "USD"), PaymentMethod.Wire);

        // Assert
        creditLine.State.CurrentBalance.Amount.Should().Be(3000m);
    }

    [Fact]
    public void ApplyPayment_ShouldIncreaseAvailableCredit()
    {
        // Arrange
        var creditLine = CreateValidCreditLine(creditLimit: 10000m);
        creditLine.Activate();
        creditLine.DrawFunds(new Money(5000m, "USD"), "Purchase");

        // Act
        creditLine.ApplyPayment(Guid.NewGuid(), new Money(2000m, "USD"), PaymentMethod.Wire);

        // Assert
        creditLine.State.AvailableCredit.Amount.Should().Be(7000m);
    }

    [Fact]
    public void ApplyPayment_ZeroAmount_ShouldThrow()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();

        // Act
        var act = () => creditLine.ApplyPayment(
            Guid.NewGuid(),
            new Money(0m, "USD"),
            PaymentMethod.Wire);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*must be greater than zero*");
    }

    [Fact]
    public void ApplyPayment_WhenClosed_ShouldThrow()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();
        creditLine.Close("Customer request");

        // Act
        var act = () => creditLine.ApplyPayment(
            Guid.NewGuid(),
            new Money(100m, "USD"),
            PaymentMethod.Wire);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*closed*");
    }

    #endregion

    #region AccrueInterest Tests

    [Fact]
    public void AccrueInterest_ShouldIncreaseAccruedInterest()
    {
        // Arrange
        var creditLine = CreateValidCreditLine(rate: 36.5m); // 0.1% daily
        creditLine.Activate();
        creditLine.DrawFunds(new Money(10000m, "USD"), "Purchase");
        var start = DateTime.UtcNow.AddDays(-10);
        var end = DateTime.UtcNow;

        // Act
        creditLine.AccrueInterest(start, end);

        // Assert
        creditLine.State.AccruedInterest.Amount.Should().BeGreaterThan(0);
        // 10 days * 0.1% * 10000 = 100
        creditLine.State.AccruedInterest.Amount.Should().Be(100m);
    }

    [Fact]
    public void AccrueInterest_WithZeroBalance_ShouldNotAccrue()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();
        // No draws, balance is 0
        var initialEventCount = creditLine.UncommittedEvents.Count;

        // Act
        creditLine.AccrueInterest(DateTime.UtcNow.AddDays(-10), DateTime.UtcNow);

        // Assert
        creditLine.State.AccruedInterest.Amount.Should().Be(0);
        creditLine.UncommittedEvents.Count.Should().Be(initialEventCount); // No new event
    }

    #endregion

    #region Freeze/Unfreeze Tests

    [Fact]
    public void Freeze_ShouldChangeStatusToFrozen()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();

        // Act
        creditLine.Freeze("Suspicious activity");

        // Assert
        creditLine.State.Status.Should().Be(RevolvingCreditStatus.Frozen);
        creditLine.State.FrozenAt.Should().NotBeNull();
    }

    [Fact]
    public void Freeze_WhenNotActive_ShouldThrow()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        // Status is Pending

        // Act
        var act = () => creditLine.Freeze("Test");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot freeze*");
    }

    [Fact]
    public void Unfreeze_ShouldChangeStatusToActive()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();
        creditLine.Freeze("Temporary freeze");

        // Act
        creditLine.Unfreeze("Issue resolved");

        // Assert
        creditLine.State.Status.Should().Be(RevolvingCreditStatus.Active);
        creditLine.State.FrozenAt.Should().BeNull();
    }

    [Fact]
    public void Unfreeze_WhenNotFrozen_ShouldThrow()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();
        // Status is Active

        // Act
        var act = () => creditLine.Unfreeze("Test");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot unfreeze*");
    }

    [Fact]
    public void DrawFunds_WhenFrozen_ShouldThrow()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();
        creditLine.Freeze("Suspicious activity");

        // Act
        var act = () => creditLine.DrawFunds(new Money(1000m, "USD"), "Purchase");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot draw funds*");
    }

    #endregion

    #region ChangeCreditLimit Tests

    [Fact]
    public void ChangeCreditLimit_ShouldUpdateLimit()
    {
        // Arrange
        var creditLine = CreateValidCreditLine(creditLimit: 10000m);
        creditLine.Activate();

        // Act
        creditLine.ChangeCreditLimit(new Money(15000m, "USD"), "Good payment history");

        // Assert
        creditLine.State.CreditLimit.Amount.Should().Be(15000m);
    }

    [Fact]
    public void ChangeCreditLimit_ShouldUpdateAvailableCredit()
    {
        // Arrange
        var creditLine = CreateValidCreditLine(creditLimit: 10000m);
        creditLine.Activate();
        creditLine.DrawFunds(new Money(3000m, "USD"), "Purchase");

        // Act
        creditLine.ChangeCreditLimit(new Money(15000m, "USD"), "Increase");

        // Assert
        creditLine.State.AvailableCredit.Amount.Should().Be(12000m); // 15000 - 3000
    }

    [Fact]
    public void ChangeCreditLimit_BelowCurrentBalance_ShouldThrow()
    {
        // Arrange
        var creditLine = CreateValidCreditLine(creditLimit: 10000m);
        creditLine.Activate();
        creditLine.DrawFunds(new Money(6000m, "USD"), "Big purchase");

        // Act
        var act = () => creditLine.ChangeCreditLimit(new Money(5000m, "USD"), "Decrease");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*cannot be less than current balance*");
    }

    [Fact]
    public void ChangeCreditLimit_WhenClosed_ShouldThrow()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();
        creditLine.Close("Customer request");

        // Act
        var act = () => creditLine.ChangeCreditLimit(new Money(20000m, "USD"), "Test");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*closed*");
    }

    #endregion

    #region Close Tests

    [Fact]
    public void Close_ShouldChangeStatusToClosed()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();

        // Act
        creditLine.Close("Customer request");

        // Assert
        creditLine.State.Status.Should().Be(RevolvingCreditStatus.Closed);
        creditLine.State.ClosedAt.Should().NotBeNull();
        creditLine.State.AvailableCredit.Amount.Should().Be(0);
    }

    [Fact]
    public void Close_WithBalance_ShouldThrow()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();
        creditLine.DrawFunds(new Money(1000m, "USD"), "Purchase");

        // Act
        var act = () => creditLine.Close("Customer request");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*balance must be zero*");
    }

    [Fact]
    public void Close_WhenAlreadyClosed_ShouldNotAddEvent()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();
        creditLine.Close("First close");
        var eventCountAfterFirst = creditLine.UncommittedEvents.Count;

        // Act
        creditLine.Close("Second close");

        // Assert
        creditLine.UncommittedEvents.Count.Should().Be(eventCountAfterFirst);
    }

    #endregion

    #region GenerateStatement Tests

    [Fact]
    public void GenerateStatement_ShouldSetPaymentDueDate()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();
        creditLine.DrawFunds(new Money(1000m, "USD"), "Purchase");

        // Act
        creditLine.GenerateStatement();

        // Assert
        creditLine.State.PaymentDueDate.Should().NotBeNull();
        creditLine.State.CurrentMinimumPayment.Should().NotBeNull();
    }

    [Fact]
    public void GenerateStatement_WithNoBalance_ShouldHaveZeroMinimum()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();
        // No draws

        // Act
        creditLine.GenerateStatement();

        // Assert
        creditLine.State.CurrentMinimumPayment!.Amount.Should().Be(0);
    }

    #endregion

    #region Event Sourcing Tests

    [Fact]
    public void Rehydrate_ShouldRestoreStateFromEvents()
    {
        // Arrange
        var original = CreateValidCreditLine(creditLimit: 10000m);
        original.Activate();
        original.DrawFunds(new Money(3000m, "USD"), "Purchase");
        original.ApplyPayment(Guid.NewGuid(), new Money(500m, "USD"), PaymentMethod.Wire);

        var events = original.UncommittedEvents.ToList();

        // Act
        var rehydrated = new RevolvingCreditAggregate(null, events);

        // Assert
        rehydrated.Id.Should().Be(original.Id);
        rehydrated.State.Status.Should().Be(original.State.Status);
        rehydrated.State.CurrentBalance.Amount.Should().Be(original.State.CurrentBalance.Amount);
        rehydrated.State.AvailableCredit.Amount.Should().Be(original.State.AvailableCredit.Amount);
    }

    [Fact]
    public void ClearUncommittedEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var creditLine = CreateValidCreditLine();
        creditLine.Activate();
        creditLine.UncommittedEvents.Should().HaveCountGreaterThan(1);

        // Act
        creditLine.ClearUncommittedEvents();

        // Assert
        creditLine.UncommittedEvents.Should().BeEmpty();
    }

    #endregion
}
