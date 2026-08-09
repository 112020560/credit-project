using CreditSystem.Domain.Aggregates.CooperativeMember;
using CreditSystem.Domain.Aggregates.CooperativeMember.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.ValueObjects;
using FluentAssertions;

namespace CreditSystem.Tests.Domain.Aggregates;

public class CooperativeMemberAggregateTests
{
    private static CooperativeMemberAggregate CreateActiveMember(
        decimal sharesAmount = 5000m,
        string currency = "CRC")
    {
        return CooperativeMemberAggregate.Register(
            externalId: Guid.NewGuid(),
            memberNumber: "SOC-001",
            joinedAt: DateTime.UtcNow.AddYears(-1),
            initialShares: new MemberShare(new Money(sharesAmount, currency), 3, DateTime.UtcNow.AddMonths(-1)));
    }

    #region Register Tests

    [Fact]
    public void Register_ShouldCreateMemberInActiveStatus()
    {
        var member = CreateActiveMember();

        member.State.Status.Should().Be(MemberStatus.Active);
        member.State.MemberNumber.Should().Be("SOC-001");
    }

    [Fact]
    public void Register_ShouldEmitMemberRegisteredEvent()
    {
        var member = CreateActiveMember();

        member.UncommittedEvents.Should().HaveCount(1);
        member.UncommittedEvents[0].Should().BeOfType<MemberRegistered>();
    }

    [Fact]
    public void Register_ShouldSetInitialShares()
    {
        var member = CreateActiveMember(sharesAmount: 10000m);

        member.State.Shares.TotalAmount.Amount.Should().Be(10000m);
        member.State.Shares.NumberOfContributions.Should().Be(3);
    }

    #endregion

    #region Suspend Tests

    [Fact]
    public void Suspend_WhenActive_ShouldTransitionToSuspended()
    {
        var member = CreateActiveMember();

        member.Suspend("Mora en aportaciones");

        member.State.Status.Should().Be(MemberStatus.Suspended);
        member.UncommittedEvents.OfType<MemberSuspended>().Should().HaveCount(1);
    }

    [Fact]
    public void Suspend_WhenAlreadySuspended_ShouldThrowDomainException()
    {
        var member = CreateActiveMember();
        member.Suspend("Primera suspensión");

        var act = () => member.Suspend("Segunda suspensión");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Suspend_WhenWithdrawn_ShouldThrowDomainException()
    {
        var member = CreateActiveMember();
        member.Withdraw("Retiro voluntario");

        var act = () => member.Suspend("Intento post-retiro");

        act.Should().Throw<DomainException>();
    }

    #endregion

    #region Reinstate Tests

    [Fact]
    public void Reinstate_WhenSuspended_ShouldTransitionToActive()
    {
        var member = CreateActiveMember();
        member.Suspend("Motivo de suspensión");

        member.Reinstate("Deuda saldada");

        member.State.Status.Should().Be(MemberStatus.Active);
        member.UncommittedEvents.OfType<MemberReinstated>().Should().HaveCount(1);
    }

    [Fact]
    public void Reinstate_WhenActive_ShouldThrowDomainException()
    {
        var member = CreateActiveMember();

        var act = () => member.Reinstate("Sin motivo");

        act.Should().Throw<DomainException>();
    }

    #endregion

    #region Withdraw Tests

    [Fact]
    public void Withdraw_WhenActive_ShouldTransitionToWithdrawn()
    {
        var member = CreateActiveMember();

        member.Withdraw("Retiro voluntario");

        member.State.Status.Should().Be(MemberStatus.Withdrawn);
        member.UncommittedEvents.OfType<MemberWithdrawn>().Should().HaveCount(1);
    }

    [Fact]
    public void Withdraw_WhenSuspended_ShouldTransitionToWithdrawn()
    {
        var member = CreateActiveMember();
        member.Suspend("Suspensión previa");

        member.Withdraw("Retiro post-suspensión");

        member.State.Status.Should().Be(MemberStatus.Withdrawn);
    }

    [Fact]
    public void Withdraw_WhenAlreadyWithdrawn_ShouldThrowDomainException()
    {
        var member = CreateActiveMember();
        member.Withdraw("Primer retiro");

        var act = () => member.Withdraw("Segundo retiro");

        act.Should().Throw<DomainException>();
    }

    #endregion

    #region UpdateShares Tests

    [Fact]
    public void UpdateShares_ShouldUpdateSharesAndEmitEvent()
    {
        var member = CreateActiveMember(sharesAmount: 5000m);
        var newShares = new MemberShare(new Money(8000m, "CRC"), 4, DateTime.UtcNow);

        member.UpdateShares(newShares);

        member.State.Shares.TotalAmount.Amount.Should().Be(8000m);
        member.State.Shares.NumberOfContributions.Should().Be(4);
        member.UncommittedEvents.OfType<MemberSharesUpdated>().Should().HaveCount(1);
    }

    #endregion

    #region Rehydration Tests

    [Fact]
    public void Rehydration_FromSnapshotWithNoEvents_ShouldRestoreState()
    {
        var original = CreateActiveMember(sharesAmount: 7500m);
        var state = original.State with { Id = Guid.NewGuid() };

        var rehydrated = new CooperativeMemberAggregate(state, Enumerable.Empty<CreditSystem.Domain.Abstractions.Events.IDomainEvent>());

        rehydrated.State.Status.Should().Be(MemberStatus.Active);
        rehydrated.State.Shares.TotalAmount.Amount.Should().Be(7500m);
        rehydrated.UncommittedEvents.Should().BeEmpty();
    }

    #endregion
}
