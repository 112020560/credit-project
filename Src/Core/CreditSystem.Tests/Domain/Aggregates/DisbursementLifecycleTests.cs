using CreditSystem.Domain.Aggregates.LoanContract;
using CreditSystem.Domain.Aggregates.LoanContract.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.Services.Amortization;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.Aggregates;

public class DisbursementLifecycleTests
{
    private readonly FrenchAmortizationCalculator _calculator = new();

    private LoanContractAggregate CreateApprovedContract()
    {
        return LoanContractAggregate.Create(
            customerId: Guid.NewGuid(),
            principal: new Money(10000m, "USD"),
            rate: new InterestRate(12m),
            termMonths: 12,
            amortizationMethod: AmortizationMethod.French,
            calculator: _calculator,
            evaluationMetadata: new Dictionary<string, object>());
    }

    [Fact]
    public void Disburse_FromApproved_ShouldTransitionToDisbursing()
    {
        var contract = CreateApprovedContract();

        contract.Disburse("WIRE", "ACC-001");

        contract.State.Status.Should().Be(ContractStatus.Disbursing);
        contract.UncommittedEvents.OfType<LoanDisbursed>().Should().HaveCount(1);
    }

    [Fact]
    public void ConfirmDisbursement_FromDisbursing_ShouldTransitionToActive()
    {
        var contract = CreateApprovedContract();
        contract.Disburse("WIRE", "ACC-001");

        contract.ConfirmDisbursement("officer-001");

        contract.State.Status.Should().Be(ContractStatus.Active);
        contract.UncommittedEvents.OfType<DisbursementConfirmed>().Should().HaveCount(1);
    }

    [Fact]
    public void ConfirmDisbursement_ShouldEmitEventWithConfirmedBy()
    {
        var contract = CreateApprovedContract();
        contract.Disburse("WIRE", "ACC-001");

        contract.ConfirmDisbursement("officer-001");

        var evt = contract.UncommittedEvents.OfType<DisbursementConfirmed>().Single();
        evt.ConfirmedBy.Should().Be("officer-001");
        evt.DisbursedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void FailDisbursement_FromDisbursing_ShouldTransitionBackToApproved()
    {
        var contract = CreateApprovedContract();
        contract.Disburse("WIRE", "ACC-001");

        contract.FailDisbursement("Bank rejected the transfer");

        contract.State.Status.Should().Be(ContractStatus.Approved);
        contract.UncommittedEvents.OfType<DisbursementFailed>().Should().HaveCount(1);
    }

    [Fact]
    public void FailDisbursement_ShouldEmitEventWithReason()
    {
        var contract = CreateApprovedContract();
        contract.Disburse("WIRE", "ACC-001");

        contract.FailDisbursement("Invalid account number");

        var evt = contract.UncommittedEvents.OfType<DisbursementFailed>().Single();
        evt.Reason.Should().Be("Invalid account number");
        evt.FailedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Disburse_FromDisbursing_ShouldThrow()
    {
        var contract = CreateApprovedContract();
        contract.Disburse("WIRE", "ACC-001");

        var act = () => contract.Disburse("ACH", "ACC-002");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ConfirmDisbursement_FromActive_ShouldThrow()
    {
        var contract = CreateApprovedContract();
        contract.Disburse("WIRE", "ACC-001");
        contract.ConfirmDisbursement("officer-001");

        var act = () => contract.ConfirmDisbursement("officer-002");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void FailDisbursement_FromApproved_ShouldThrow()
    {
        var contract = CreateApprovedContract();

        var act = () => contract.FailDisbursement("No reason");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RetryFlow_DisburseFailDisburseConfirm_ShouldEndInActive()
    {
        var contract = CreateApprovedContract();

        contract.Disburse("WIRE", "ACC-001");
        contract.State.Status.Should().Be(ContractStatus.Disbursing);

        contract.FailDisbursement("Bank offline");
        contract.State.Status.Should().Be(ContractStatus.Approved);

        contract.Disburse("ACH", "ACC-001");
        contract.State.Status.Should().Be(ContractStatus.Disbursing);

        contract.ConfirmDisbursement("officer-001");
        contract.State.Status.Should().Be(ContractStatus.Active);
    }
}
