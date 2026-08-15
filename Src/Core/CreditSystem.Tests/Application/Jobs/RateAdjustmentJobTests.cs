using CreditSystem.Application.Job;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Domain.Entities;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Models.ReadModels;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;
using CreditSystem.Domain.Aggregates.LoanContract;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CreditSystem.Tests.Application.Jobs;

public class RateAdjustmentJobTests
{
    private readonly ILoanQueryService _queryService = Substitute.For<ILoanQueryService>();
    private readonly ILoanContractRepository _repository = Substitute.For<ILoanContractRepository>();
    private readonly IReferenceRateRepository _referenceRateRepository = Substitute.For<IReferenceRateRepository>();

    private RateAdjustmentJob CreateJob() =>
        new(_queryService, _repository, _referenceRateRepository,
            NullLogger<RateAdjustmentJob>.Instance);

    private static LoanContractAggregate CreateActiveVariableAggregate()
    {
        var calculator = new FrenchAmortizationCalculator();
        var rate = new InterestRate(12.25m, RateType.Variable, 8.0m, "TBP_CRC");
        var aggregate = LoanContractAggregate.Create(
            customerId: Guid.NewGuid(),
            principal: new Money(10_000m, "CRC"),
            rate: rate,
            termMonths: 24,
            amortizationMethod: AmortizationMethod.French,
            calculator: calculator,
            evaluationMetadata: new Dictionary<string, object>());
        aggregate.ClearUncommittedEvents();
        aggregate.Disburse("Transfer", "CR12345");
        aggregate.ConfirmDisbursement("test");
        aggregate.ClearUncommittedEvents();
        return aggregate;
    }

    [Fact]
    public async Task ExecuteAsync_WhenRateChanged_CallsSaveAsync()
    {
        var loanId = Guid.NewGuid();
        var aggregate = CreateActiveVariableAggregate();

        _queryService.GetActiveVariableRateLoansAsync(default)
            .ReturnsForAnyArgs(new List<VariableRateLoanInfo>
            {
                new() { LoanId = loanId, CurrentRate = 12.25m, Spread = 8.0m, ReferenceRateId = "TBP_CRC" }
            });

        _referenceRateRepository.GetCurrentAsync("TBP_CRC", default)
            .ReturnsForAnyArgs(new ReferenceRateEntry
            {
                Id = "TBP_CRC", Name = "TBP", CurrentValue = 5.0m,
                EffectiveDate = DateTime.UtcNow, Source = "BCCR", UpdatedAt = DateTime.UtcNow
            });

        _repository.GetByIdAsync(loanId, default)
            .ReturnsForAnyArgs(aggregate);

        await CreateJob().ExecuteAsync();

        await _repository.ReceivedWithAnyArgs(1).SaveAsync(Arg.Any<LoanContractAggregate>(), default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRateUnchanged_DoesNotCallSaveAsync()
    {
        var loanId = Guid.NewGuid();
        var aggregate = CreateActiveVariableAggregate(); // rate = 12.25 (4.25 + 8)

        _queryService.GetActiveVariableRateLoansAsync(default)
            .ReturnsForAnyArgs(new List<VariableRateLoanInfo>
            {
                new() { LoanId = loanId, CurrentRate = 12.25m, Spread = 8.0m, ReferenceRateId = "TBP_CRC" }
            });

        _referenceRateRepository.GetCurrentAsync("TBP_CRC", default)
            .ReturnsForAnyArgs(new ReferenceRateEntry
            {
                Id = "TBP_CRC", Name = "TBP", CurrentValue = 4.25m, // same as before
                EffectiveDate = DateTime.UtcNow, Source = "BCCR", UpdatedAt = DateTime.UtcNow
            });

        _repository.GetByIdAsync(loanId, default)
            .ReturnsForAnyArgs(aggregate);

        await CreateJob().ExecuteAsync();

        await _repository.DidNotReceiveWithAnyArgs().SaveAsync(Arg.Any<LoanContractAggregate>(), default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoVariableLoans_DoesNothing()
    {
        _queryService.GetActiveVariableRateLoansAsync(default)
            .ReturnsForAnyArgs(new List<VariableRateLoanInfo>());

        await CreateJob().ExecuteAsync();

        await _repository.DidNotReceiveWithAnyArgs().GetByIdAsync(Arg.Any<Guid>(), default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReferenceRateNotFound_SkipsLoan()
    {
        var loanId = Guid.NewGuid();

        _queryService.GetActiveVariableRateLoansAsync(default)
            .ReturnsForAnyArgs(new List<VariableRateLoanInfo>
            {
                new() { LoanId = loanId, CurrentRate = 12.25m, Spread = 8.0m, ReferenceRateId = "MISSING" }
            });

        _referenceRateRepository.GetCurrentAsync("MISSING", default)
            .ReturnsForAnyArgs((ReferenceRateEntry?)null);

        await CreateJob().ExecuteAsync();

        await _repository.DidNotReceiveWithAnyArgs().GetByIdAsync(Arg.Any<Guid>(), default);
        await _repository.DidNotReceiveWithAnyArgs().SaveAsync(Arg.Any<LoanContractAggregate>(), default);
    }
}
