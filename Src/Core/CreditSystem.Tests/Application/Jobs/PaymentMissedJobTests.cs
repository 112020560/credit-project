using CreditSystem.Application.Configuration;
using CreditSystem.Application.Job;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Aggregates.LoanContract;
using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Models;
using CreditSystem.Domain.Models.ReadModels;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CreditSystem.Tests.Application.Jobs;

public class PaymentMissedJobTests
{
    private readonly ILoanQueryService _queryService = Substitute.For<ILoanQueryService>();
    private readonly ILoanContractRepository _repository = Substitute.For<ILoanContractRepository>();
    private readonly IUnderwritingPolicyRepository _policyRepo = Substitute.For<IUnderwritingPolicyRepository>();
    private readonly FrenchAmortizationCalculator _calculator = new();

    private readonly LateFeeConfiguration _lateFeeConfig = new()
    {
        PercentageOfPayment = 5m,
        FixedAmount = 25m,
        DailyAmount = 0m,
        MaximumFee = 100m
    };

    private PaymentMissedJob CreateJob(UnderwritingPolicy policy)
    {
        _policyRepo.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(policy);
        return new PaymentMissedJob(
            _queryService,
            _repository,
            Options.Create(_lateFeeConfig),
            NullLogger<PaymentMissedJob>.Instance,
            _policyRepo);
    }

    private LoanContractAggregate CreateActiveAggregate(decimal principal = 10000m)
    {
        var aggregate = LoanContractAggregate.Create(
            customerId: Guid.NewGuid(),
            principal: new Money(principal, "USD"),
            rate: new InterestRate(12m),
            termMonths: 12,
            amortizationMethod: AmortizationMethod.French,
            calculator: _calculator,
            evaluationMetadata: new Dictionary<string, object>());

        aggregate.Disburse("WIRE", "123");
        aggregate.ConfirmDisbursement("test");
        aggregate.ClearUncommittedEvents();
        return aggregate;
    }

    [Fact]
    public async Task ExecuteAsync_LoanWithinGracePeriod_ShouldSkipWithoutRecordingMissedPayment()
    {
        // Arrange — loan is only 3 days overdue; grace period is 5
        var policy = new UnderwritingPolicy(8m, 90, NoScoreBehavior.ApproveWithPenalty,
            GracePeriodDays: 5, PenaltyRate: 0m, OriginationFeeRate: 0m);

        var aggregate = CreateActiveAggregate();
        var loanId = aggregate.Id;

        var overdueLoans = new List<OverdueLoanInfo>
        {
            new()
            {
                LoanId = loanId,
                CustomerId = Guid.NewGuid(),
                PaymentNumber = 1,
                DueDate = DateTime.UtcNow.AddDays(-3),
                AmountDue = 900m,
                Currency = "USD",
                DaysOverdue = 3
            }
        };

        _queryService.GetLoansWithOverduePaymentsAsync(Arg.Any<CancellationToken>())
            .Returns(overdueLoans);
        _repository.GetByIdAsync(loanId, Arg.Any<CancellationToken>())
            .Returns(aggregate);

        var job = CreateJob(policy);

        // Act
        await job.ExecuteAsync();

        // Assert: aggregate was loaded but RecordMissedPayment was NOT called
        // (no save, no projection)
        await _repository.DidNotReceive().SaveAsync(Arg.Any<LoanContractAggregate>(), Arg.Any<CancellationToken>());
        aggregate.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_LoanPastGracePeriod_ShouldRecordMissedPaymentWithPenaltyInterest()
    {
        // Arrange — loan is 30 days overdue; grace = 5; penalty rate = 36.5% annual
        var policy = new UnderwritingPolicy(8m, 90, NoScoreBehavior.ApproveWithPenalty,
            GracePeriodDays: 5, PenaltyRate: 36.5m, OriginationFeeRate: 0m);

        var principal = 10000m;
        var aggregate = CreateActiveAggregate(principal);
        var loanId = aggregate.Id;

        var daysOverdue = 30;
        var overdueLoans = new List<OverdueLoanInfo>
        {
            new()
            {
                LoanId = loanId,
                CustomerId = Guid.NewGuid(),
                PaymentNumber = 1,
                DueDate = DateTime.UtcNow.AddDays(-daysOverdue),
                AmountDue = 900m,
                Currency = "USD",
                DaysOverdue = daysOverdue
            }
        };

        _queryService.GetLoansWithOverduePaymentsAsync(Arg.Any<CancellationToken>())
            .Returns(overdueLoans);
        _repository.GetByIdAsync(loanId, Arg.Any<CancellationToken>())
            .Returns(aggregate);

        var job = CreateJob(policy);

        // Act
        await job.ExecuteAsync();

        // Assert: SaveAsync was called
        await _repository.Received(1).SaveAsync(aggregate, Arg.Any<CancellationToken>());

        // The PaymentMissed event should contain penalty interest
        var missedEvent = aggregate.UncommittedEvents.OfType<PaymentMissed>().Single();

        // expected: 10000 × (36.5/100/365) × 30 = 10000 × 0.001 × 30 = 30
        var expectedPenalty = principal * (36.5m / 100m / 365m) * daysOverdue;
        missedEvent.PenaltyInterestAccrued.Amount.Should().BeApproximately(expectedPenalty, 0.01m);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroPenaltyRate_ShouldRecordMissedPaymentWithZeroPenaltyInterest()
    {
        // Arrange — loan is 30 days overdue; grace = 5; penalty rate = 0
        var policy = new UnderwritingPolicy(8m, 90, NoScoreBehavior.ApproveWithPenalty,
            GracePeriodDays: 5, PenaltyRate: 0m, OriginationFeeRate: 0m);

        var aggregate = CreateActiveAggregate();
        var loanId = aggregate.Id;

        var overdueLoans = new List<OverdueLoanInfo>
        {
            new()
            {
                LoanId = loanId,
                CustomerId = Guid.NewGuid(),
                PaymentNumber = 1,
                DueDate = DateTime.UtcNow.AddDays(-30),
                AmountDue = 900m,
                Currency = "USD",
                DaysOverdue = 30
            }
        };

        _queryService.GetLoansWithOverduePaymentsAsync(Arg.Any<CancellationToken>())
            .Returns(overdueLoans);
        _repository.GetByIdAsync(loanId, Arg.Any<CancellationToken>())
            .Returns(aggregate);

        var job = CreateJob(policy);

        // Act
        await job.ExecuteAsync();

        // Assert
        var missedEvent = aggregate.UncommittedEvents.OfType<PaymentMissed>().Single();
        missedEvent.PenaltyInterestAccrued.Amount.Should().Be(0m);
    }
}
