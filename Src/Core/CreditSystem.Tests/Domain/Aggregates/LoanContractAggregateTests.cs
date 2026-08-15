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

        // Assert: Create emits ContractCreated + ContractApproved
        contract.UncommittedEvents.Should().HaveCount(2);
        contract.UncommittedEvents.First().Should().BeOfType<ContractCreated>();
        contract.UncommittedEvents.Skip(1).First().Should().BeOfType<ContractApproved>();
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
    public void Disburse_WhenApproved_ShouldChangeStatusToDisbursing()
    {
        // Arrange
        var contract = CreateValidContract();

        // Act
        contract.Disburse("WIRE", "1234567890");

        // Assert
        contract.State.Status.Should().Be(ContractStatus.Disbursing);
        contract.State.DisbursedAt.Should().NotBeNull();
    }

    [Fact]
    public void Disburse_ShouldGenerateLoanDisbursedEvent()
    {
        // Arrange
        var contract = CreateValidContract();

        // Act
        contract.Disburse("ACH", "0987654321");

        // Assert: Create (2 events) + Disburse (1 event) = 3 total
        contract.UncommittedEvents.Should().HaveCount(3);
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
        contract.ConfirmDisbursement("test");
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
        contract.ConfirmDisbursement("test");
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
        contract.ConfirmDisbursement("test");

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
        contract.ConfirmDisbursement("test");
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
        contract.ConfirmDisbursement("test");

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
        contract.ConfirmDisbursement("test");

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
        contract.ConfirmDisbursement("test");

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
        contract.ConfirmDisbursement("test");

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
        contract.ConfirmDisbursement("test");
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
        contract.ConfirmDisbursement("test");

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
        contract.ConfirmDisbursement("test");

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
        contract.ConfirmDisbursement("test");
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
        contract.ConfirmDisbursement("test");
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
        contract.ConfirmDisbursement("test");
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
        contract.ConfirmDisbursement("test");
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
        contract.ConfirmDisbursement("test");
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

    #region Origination Fee Tests

    [Fact]
    public void Create_WithOriginationFee_ShouldInitializeTotalFees()
    {
        // Arrange
        var fee = new Money(250m, "USD");

        // Act
        var contract = LoanContractAggregate.Create(
            customerId: Guid.NewGuid(),
            principal: new Money(10000m, "USD"),
            rate: new InterestRate(12m),
            termMonths: 12,
            amortizationMethod: AmortizationMethod.French,
            calculator: _calculator,
            evaluationMetadata: new Dictionary<string, object>(),
            originationFee: fee);

        // Assert
        contract.State.TotalFees.Amount.Should().Be(250m);
        contract.State.OriginationFee.Amount.Should().Be(250m);
    }

    [Fact]
    public void Create_WithoutOriginationFee_ShouldHaveZeroFees()
    {
        // Act
        var contract = CreateValidContract();

        // Assert
        contract.State.TotalFees.Amount.Should().Be(0m);
        contract.State.OriginationFee.Amount.Should().Be(0m);
    }

    #endregion

    #region Penalty Interest Tests

    [Fact]
    public void RecordMissedPayment_WithPenaltyInterest_ShouldAccumulateAccruedPenaltyInterest()
    {
        // Arrange
        var contract = CreateValidContract(principal: 10000m);
        contract.Disburse("WIRE", "123");
        var penalty = new Money(15m, "USD");

        // Act
        contract.RecordMissedPayment(1, DateTime.UtcNow.AddDays(-35), new Money(25m, "USD"), penaltyInterest: penalty);

        // Assert
        contract.State.AccruedPenaltyInterest.Amount.Should().Be(15m);
    }

    [Fact]
    public void RecordMissedPayment_WithoutPenaltyInterest_ShouldNotChangeAccruedPenaltyInterest()
    {
        // Arrange
        var contract = CreateValidContract(principal: 10000m);
        contract.Disburse("WIRE", "123");

        // Act
        contract.RecordMissedPayment(1, DateTime.UtcNow.AddDays(-35), new Money(25m, "USD"));

        // Assert
        contract.State.AccruedPenaltyInterest.Amount.Should().Be(0m);
    }

    [Fact]
    public void ApplyPayment_WithPenaltyInterest_ShouldApplyFeesFirst_ThenPenalty_ThenInterest_ThenPrincipal()
    {
        // Arrange
        var contract = LoanContractAggregate.Create(
            customerId: Guid.NewGuid(),
            principal: new Money(10000m, "USD"),
            rate: new InterestRate(12m),
            termMonths: 12,
            amortizationMethod: AmortizationMethod.French,
            calculator: _calculator,
            evaluationMetadata: new Dictionary<string, object>(),
            originationFee: new Money(100m, "USD")); // TotalFees = 100

        contract.Disburse("WIRE", "123");

        // Accrue some regular interest
        contract.AccrueInterest(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        // Record missed payment with penalty interest
        contract.RecordMissedPayment(1, DateTime.UtcNow.AddDays(-35), Money.Zero("USD"), penaltyInterest: new Money(50m, "USD"));

        var totalFeesBefore = contract.State.TotalFees.Amount;          // 100 (origination) + 0 (late fee)
        var penaltyBefore = contract.State.AccruedPenaltyInterest.Amount; // 50
        var interestBefore = contract.State.AccruedInterest.Amount;

        // Act: pay exactly enough to cover fees + penalty (100 + 50 = 150)
        contract.ApplyPayment(Guid.NewGuid(), new Money(150m, "USD"), PaymentMethod.Wire);

        // Assert: fees fully paid, penalty fully paid, interest untouched
        contract.State.TotalFees.Amount.Should().Be(0m);
        contract.State.AccruedPenaltyInterest.Amount.Should().Be(0m);
        contract.State.AccruedInterest.Amount.Should().Be(interestBefore); // not reduced
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
        var rehydratedContract = new LoanContractAggregate(null, events);

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

    #region Waterfall and Social Capital Tests

    [Fact]
    public void ApplyPayment_WithCustomWaterfall_ShouldApplyInConfiguredOrder()
    {
        // Arrange: principal-first waterfall
        var contract = LoanContractAggregate.Create(
            customerId: Guid.NewGuid(),
            principal: new Money(10000m, "USD"),
            rate: new InterestRate(12m),
            termMonths: 12,
            amortizationMethod: AmortizationMethod.French,
            calculator: _calculator,
            evaluationMetadata: new Dictionary<string, object>());

        contract.Disburse("WIRE", "123");
        contract.AccrueInterest(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        var interestBefore = contract.State.AccruedInterest.Amount;
        var balanceBefore = contract.State.CurrentBalance.Amount;

        var principalFirstWaterfall = new PaymentWaterfall([
            new PaymentWaterfallStep(1, PaymentComponent.Principal),
            new PaymentWaterfallStep(2, PaymentComponent.RegularInterest)
        ]);

        // Act: pay 500 — all goes to principal first
        contract.ApplyPayment(Guid.NewGuid(), new Money(500m, "USD"), PaymentMethod.Wire,
            waterfall: principalFirstWaterfall);

        // Assert: principal reduced first, interest untouched
        contract.State.CurrentBalance.Amount.Should().Be(balanceBefore - 500m);
        contract.State.AccruedInterest.Amount.Should().Be(interestBefore);
    }

    [Fact]
    public void ApplyPayment_WithSocialCapital_SeparateCollection_ShouldNotReduceLoanAmount()
    {
        // Arrange
        var contract = CreateValidContract(principal: 10000m);
        contract.Disburse("WIRE", "123");
        var balanceBefore = contract.State.CurrentBalance.Amount;

        var socialCapital = new Money(500m, "USD");

        // Act: 1000 payment, 500 social capital (SeparateCollection = loan gets full 1000)
        contract.ApplyPayment(Guid.NewGuid(), new Money(1000m, "USD"), PaymentMethod.Wire,
            socialCapitalContributed: socialCapital,
            collectionMode: SocialCapitalCollectionMode.SeparateCollection);

        var paymentEvent = contract.UncommittedEvents.OfType<PaymentApplied>().Last();

        // Assert: full 1000 applied to loan, social capital recorded informatively
        paymentEvent.SocialCapitalContributed.Amount.Should().Be(500m);
        paymentEvent.PrincipalPaid.Amount.Should().BeGreaterThan(0m);
        // Total loan reduction should be based on full 1000, not 500
        (balanceBefore - contract.State.CurrentBalance.Amount).Should().BeGreaterThan(500m);
    }

    [Fact]
    public void ApplyPayment_WithSocialCapital_IncludedInPayment_ShouldDeductFromLoanAmount()
    {
        // Arrange
        var contract = CreateValidContract(principal: 10000m);
        contract.Disburse("WIRE", "123");
        var balanceBefore = contract.State.CurrentBalance.Amount;

        var socialCapital = new Money(200m, "USD");

        // Act: 1000 payment, 200 is social capital (IncludedInPayment = loan gets 800)
        contract.ApplyPayment(Guid.NewGuid(), new Money(1000m, "USD"), PaymentMethod.Wire,
            socialCapitalContributed: socialCapital,
            collectionMode: SocialCapitalCollectionMode.IncludedInPayment);

        var paymentEvent = contract.UncommittedEvents.OfType<PaymentApplied>().Last();

        // Assert: social capital recorded; loan principal reduction is based on 800 (less than with SeparateCollection)
        paymentEvent.SocialCapitalContributed.Amount.Should().Be(200m);
        (balanceBefore - contract.State.CurrentBalance.Amount).Should().BeLessThan(1000m);
    }

    [Fact]
    public void ApplyPayment_WithSocialCapital_ShouldAccumulateTotalSocialCapitalContributed()
    {
        // Arrange
        var contract = CreateValidContract(principal: 10000m);
        contract.Disburse("WIRE", "123");

        // Act: two payments each with 300 social capital
        var sc = new Money(300m, "USD");
        contract.ApplyPayment(Guid.NewGuid(), new Money(1000m, "USD"), PaymentMethod.Wire, socialCapitalContributed: sc);
        contract.ApplyPayment(Guid.NewGuid(), new Money(1000m, "USD"), PaymentMethod.Wire, socialCapitalContributed: sc);

        // Assert
        contract.State.TotalSocialCapitalContributed.Amount.Should().Be(600m);
    }

    [Fact]
    public void PaymentWaterfall_Default_ShouldPreserveFeesFirst_ThenInterest_ThenPrincipal()
    {
        // Arrange
        var contract = LoanContractAggregate.Create(
            customerId: Guid.NewGuid(),
            principal: new Money(10000m, "USD"),
            rate: new InterestRate(12m),
            termMonths: 12,
            amortizationMethod: AmortizationMethod.French,
            calculator: _calculator,
            evaluationMetadata: new Dictionary<string, object>(),
            originationFee: new Money(100m, "USD"));

        contract.Disburse("WIRE", "123");
        contract.AccrueInterest(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        var feesBefore = contract.State.TotalFees.Amount;    // 100
        var interestBefore = contract.State.AccruedInterest.Amount;

        // Act: pay just over fees only — 120 (100 fee + 20 interest)
        contract.ApplyPayment(Guid.NewGuid(), new Money(120m, "USD"), PaymentMethod.Wire,
            waterfall: PaymentWaterfall.Default);

        // Assert: fees fully consumed first, then interest partially
        contract.State.TotalFees.Amount.Should().Be(0m);
        contract.State.AccruedInterest.Amount.Should().Be(interestBefore - 20m);
    }

    [Fact]
    public void SocialCapitalConfig_FixedAmount_ShouldReturnConfiguredValue()
    {
        var config = new SocialCapitalConfig
        {
            CalculationType = SocialCapitalCalculationType.FixedAmount,
            Value = 500m,
            CollectionMode = SocialCapitalCollectionMode.SeparateCollection
        };

        var result = config.Calculate(
            paymentAmount: new Money(5000m, "CRC"),
            principalPaid: Money.Zero("CRC"),
            originalAmount: new Money(100000m, "CRC"),
            currency: "CRC");

        result.Amount.Should().Be(500m);
    }

    [Fact]
    public void SocialCapitalConfig_PercentageOfPayment_ShouldCalculateCorrectly()
    {
        var config = new SocialCapitalConfig
        {
            CalculationType = SocialCapitalCalculationType.PercentageOfPayment,
            Value = 2m,  // 2%
            CollectionMode = SocialCapitalCollectionMode.IncludedInPayment
        };

        var result = config.Calculate(
            paymentAmount: new Money(10000m, "CRC"),
            principalPaid: Money.Zero("CRC"),
            originalAmount: new Money(500000m, "CRC"),
            currency: "CRC");

        result.Amount.Should().Be(200m); // 2% of 10000
    }

    #endregion
}
