using CreditSystem.Application.Commands.ConfirmDisbursement;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Aggregates.LoanContract;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CreditSystem.Tests.Commands;

public class ConfirmDisbursementCommandHandlerTests
{
    private readonly ILoanContractRepository _repository = Substitute.For<ILoanContractRepository>();
    private readonly ConfirmDisbursementCommandHandler _handler;
    private readonly FrenchAmortizationCalculator _calculator = new();

    public ConfirmDisbursementCommandHandlerTests()
    {
        _handler = new ConfirmDisbursementCommandHandler(
            _repository,
            NullLogger<ConfirmDisbursementCommandHandler>.Instance);
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
    public async Task Handle_WhenLoanInDisbursing_ShouldConfirmAndReturnSuccess()
    {
        var aggregate = CreateDisbursingAggregate();
        var command = new ConfirmDisbursementCommand
        {
            LoanId = aggregate.Id,
            ConfirmedBy = "officer-001"
        };

        _repository.GetByIdAsync(aggregate.Id, Arg.Any<CancellationToken>()).Returns(aggregate);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.LoanId.Should().Be(aggregate.Id);
        result.ConfirmedBy.Should().Be("officer-001");
        result.DisbursedAt.Should().NotBeNull();

        await _repository.Received(1).SaveAsync(aggregate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLoanNotFound_ShouldReturnFailure()
    {
        var command = new ConfirmDisbursementCommand
        {
            LoanId = Guid.NewGuid(),
            ConfirmedBy = "officer-001"
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

        // Aggregate is Approved, not Disbursing — ConfirmDisbursement should throw
        var command = new ConfirmDisbursementCommand
        {
            LoanId = aggregate.Id,
            ConfirmedBy = "officer-001"
        };

        _repository.GetByIdAsync(aggregate.Id, Arg.Any<CancellationToken>()).Returns(aggregate);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();

        await _repository.DidNotReceive().SaveAsync(Arg.Any<LoanContractAggregate>(), Arg.Any<CancellationToken>());
    }
}
