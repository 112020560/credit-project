using CreditSystem.Domain.Aggregates.LoanContract;
using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.Aggregates;

public class RateAdjustmentTests
{
    private readonly FrenchAmortizationCalculator _calculator = new();

    private LoanContractAggregate CreateVariableContract(
        decimal effectiveRate = 12.25m,
        decimal spread = 8.0m,
        string referenceRateId = "TBP_CRC",
        int termMonths = 24)
    {
        var rate = new InterestRate(effectiveRate, RateType.Variable, spread, referenceRateId);
        var aggregate = LoanContractAggregate.Create(
            customerId: Guid.NewGuid(),
            principal: new Money(10_000m, "CRC"),
            rate: rate,
            termMonths: termMonths,
            amortizationMethod: AmortizationMethod.French,
            calculator: _calculator,
            evaluationMetadata: new Dictionary<string, object>());

        // Disburse and confirm to make it Active
        aggregate.ClearUncommittedEvents();
        aggregate.Disburse("Transfer", "CR12345");
        aggregate.ConfirmDisbursement("test");
        aggregate.ClearUncommittedEvents();
        return aggregate;
    }

    [Fact]
    public void AdjustRate_WhenRateChanges_EmitsRateAdjustedWithNewSchedule()
    {
        var aggregate = CreateVariableContract(effectiveRate: 12.25m, spread: 8.0m);

        // TBP moves from 4.25 to 5.00 → new effective rate = 13.00
        aggregate.AdjustRate(newReferenceRateValue: 5.0m, adjustedAt: DateTime.UtcNow);

        var rateEvent = aggregate.UncommittedEvents.OfType<RateAdjusted>().Single();
        rateEvent.OldRate.Should().Be(12.25m);
        rateEvent.NewRate.Should().Be(13.0m);
        rateEvent.ReferenceRateId.Should().Be("TBP_CRC");
        rateEvent.ReferenceRateValue.Should().Be(5.0m);
        rateEvent.NewSchedule.Should().NotBeNull();
        rateEvent.NewSchedule.Entries.Should().NotBeEmpty();
    }

    [Fact]
    public void AdjustRate_WhenRateChanges_StateReflectsNewRate()
    {
        var aggregate = CreateVariableContract(effectiveRate: 12.25m, spread: 8.0m);

        aggregate.AdjustRate(newReferenceRateValue: 5.0m, adjustedAt: DateTime.UtcNow);

        aggregate.State.InterestRate.AnnualRate.Should().Be(13.0m);
        aggregate.State.Schedule.Entries.Should().NotBeEmpty();
    }

    [Fact]
    public void AdjustRate_WhenRateUnchanged_EmitsNoEvent()
    {
        // effectiveRate = spread(8) + referenceRate(4.25) = 12.25
        var aggregate = CreateVariableContract(effectiveRate: 12.25m, spread: 8.0m);

        // Same reference rate value → no change
        aggregate.AdjustRate(newReferenceRateValue: 4.25m, adjustedAt: DateTime.UtcNow);

        aggregate.UncommittedEvents.OfType<RateAdjusted>().Should().BeEmpty();
    }

    [Fact]
    public void AdjustRate_OnFixedLoan_ThrowsDomainException()
    {
        var fixedRate = new InterestRate(12m);
        var aggregate = LoanContractAggregate.Create(
            customerId: Guid.NewGuid(),
            principal: new Money(10_000m, "CRC"),
            rate: fixedRate,
            termMonths: 12,
            amortizationMethod: AmortizationMethod.French,
            calculator: _calculator,
            evaluationMetadata: new Dictionary<string, object>());

        aggregate.ClearUncommittedEvents();
        aggregate.Disburse("Transfer", "CR12345");
        aggregate.ClearUncommittedEvents();

        var act = () => aggregate.AdjustRate(5.0m, DateTime.UtcNow);

        act.Should().Throw<DomainException>()
            .WithMessage("*fixed*");
    }

    [Fact]
    public void AdjustRate_WhenRateChangeIsNearZero_EmitsNoEvent()
    {
        // Difference of 0.00005 is below threshold of 0.0001
        var aggregate = CreateVariableContract(effectiveRate: 12.25m, spread: 8.0m);

        // newEffectiveRate = 4.25000 + 8 = 12.25000, difference = 0.00005 < 0.0001
        aggregate.AdjustRate(newReferenceRateValue: 4.25005m, adjustedAt: DateTime.UtcNow);

        aggregate.UncommittedEvents.OfType<RateAdjusted>().Should().BeEmpty();
    }
}
