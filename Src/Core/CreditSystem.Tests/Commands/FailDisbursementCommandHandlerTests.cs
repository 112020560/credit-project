using CreditSystem.Application.Commands.FailDisbursement;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Aggregates.LoanContract;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CreditSystem.Tests.Commands;

public class FailDisbursementCommandHandlerTests
{
    private readonly ILoanContractRepository _repository = Substitute.For<ILoanContractRepository>();
    private readonly FailDisbursementCommandHandler _handler;
    private readonly FrenchAmortizationCalculator _calculator = new();

    public FailDisbursementCommandHandlerTests()
    {
        _handler = new FailDisbursementCommandHandler(
            _repository,
            NullLogger<FailDisbursementCommandHandler>.Instance);
    }

    private LoanContractAggregate CreateDisbursingAggregate()
    {
        var aggregate = LoanContractAggregate.Create(
            customerId: Guid.NewGuid(),
            principal: new Money(10000m, "USD"),
            rate: new InterestRate(12m),
            termMonths: 12,
            amortizationMethod: AmortizationMethod.French,
            calculator: _calculator,
            evaluationMetadata: new Dictionary<string, object>());

        aggregate.Disburse("WIRE", "ACC-001");
        aggregate.ClearUncommittedEvents();
        return aggregate;
    }

    [Fact]
    public async Task Handle_WhenLoanInDisbursing_ShouldFailAndReturnSuccess()
    {
        var aggregate = CreateDisbursingAggregate();
        var command = new FailDisbursementCommand
        {
            LoanId = aggregate.Id,
            Reason = "Bank rejected the transfer"
        };

        _repository.GetByIdAsync(aggregate.Id, Arg.Any<CancellationToken>()).Returns(aggregate);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.LoanId.Should().Be(aggregate.Id);
        result.Reason.Should().Be("Bank rejected the transfer");
        result.FailedAt.Should().NotBeNull();

        await _repository.Received(1).SaveAsync(aggregate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLoanNotFound_ShouldReturnFailure()
    {
        var command = new FailDisbursementCommand
        {
            LoanId = Guid.NewGuid(),
            Reason = "Network error"
        };

        _repository.GetByIdAsync(command.LoanId, Arg.Any<CancellationToken>())
            .Returns((LoanContractAggregate?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");

        await _repository.DidNotReceive().SaveAsync(Arg.Any<LoanContractAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDomainExceptionThrown_ShouldReturnFailure()
    {
        var aggregate = LoanContractAggregate.Create(
            customerId: Guid.NewGuid(),
            principal: new Money(10000m, "USD"),
            rate: new InterestRate(12m),
            termMonths: 12,
            amortizationMethod: AmortizationMethod.French,
            calculator: _calculator,
            evaluationMetadata: new Dictionary<string, object>());

        // Aggregate is Approved, not Disbursing — FailDisbursement should throw
        var command = new FailDisbursementCommand
        {
            LoanId = aggregate.Id,
            Reason = "Some reason"
        };

        _repository.GetByIdAsync(aggregate.Id, Arg.Any<CancellationToken>()).Returns(aggregate);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();

        await _repository.DidNotReceive().SaveAsync(Arg.Any<LoanContractAggregate>(), Arg.Any<CancellationToken>());
    }
}
