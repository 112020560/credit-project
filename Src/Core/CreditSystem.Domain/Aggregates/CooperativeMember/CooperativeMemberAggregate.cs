using CreditSystem.Domain.Abstractions.Events;
using CreditSystem.Domain.Aggregates.CooperativeMember.Events;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.CooperativeMember;

public class CooperativeMemberAggregate
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();

    public Guid Id { get; private set; }
    public MemberState State { get; private set; } = null!;
    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    private CooperativeMemberAggregate()
    {
        State = MemberState.Initial;
    }

    public CooperativeMemberAggregate(MemberState? snapshot, IEnumerable<IDomainEvent> events)
    {
        State = snapshot ?? MemberState.Initial;
        if (snapshot != null)
            Id = snapshot.Id;
        foreach (var @event in events)
            Apply(@event, isNew: false);
    }

    #region Factory

    public static CooperativeMemberAggregate Register(
        Guid externalId,
        string memberNumber,
        DateTime joinedAt,
        MemberShare initialShares)
    {
        var aggregate = new CooperativeMemberAggregate();
        var id = Guid.NewGuid();
        aggregate.Id = id;

        aggregate.Apply(new MemberRegistered
        {
            AggregateId = id,
            ExternalId = externalId,
            MemberNumber = memberNumber,
            JoinedAt = joinedAt,
            InitialShares = initialShares
        }, isNew: true);

        return aggregate;
    }

    #endregion

    #region Commands

    public void Suspend(string reason)
    {
        if (State.Status != MemberStatus.Active)
            throw new DomainException($"Cannot suspend member in status {State.Status}");

        Apply(new MemberSuspended
        {
            AggregateId = Id,
            Reason = reason,
            SuspendedAt = DateTime.UtcNow
        }, isNew: true);
    }

    public void Reinstate(string reason)
    {
        if (State.Status != MemberStatus.Suspended)
            throw new DomainException($"Cannot reinstate member in status {State.Status}");

        Apply(new MemberReinstated
        {
            AggregateId = Id,
            Reason = reason,
            ReinstatedAt = DateTime.UtcNow
        }, isNew: true);
    }

    public void Withdraw(string reason)
    {
        if (State.Status == MemberStatus.Withdrawn)
            throw new DomainException("Member is already withdrawn");

        Apply(new MemberWithdrawn
        {
            AggregateId = Id,
            Reason = reason,
            WithdrawnAt = DateTime.UtcNow
        }, isNew: true);
    }

    public void UpdateShares(MemberShare newShares)
    {
        Apply(new MemberSharesUpdated
        {
            AggregateId = Id,
            NewShares = newShares,
            UpdatedAt = DateTime.UtcNow
        }, isNew: true);
    }

    #endregion

    #region Apply

    private void Apply(IDomainEvent @event, bool isNew)
    {
        State = @event switch
        {
            MemberRegistered e => State with
            {
                Id = e.AggregateId,
                ExternalId = e.ExternalId,
                MemberNumber = e.MemberNumber,
                Status = MemberStatus.Active,
                JoinedAt = e.JoinedAt,
                Shares = e.InitialShares
            },
            MemberSuspended => State with { Status = MemberStatus.Suspended },
            MemberReinstated => State with { Status = MemberStatus.Active },
            MemberWithdrawn => State with { Status = MemberStatus.Withdrawn },
            MemberSharesUpdated e => State with { Shares = e.NewShares },
            _ => State
        };

        if (@event is MemberRegistered reg)
            Id = reg.AggregateId;

        if (isNew)
            _uncommittedEvents.Add(@event);
    }

    #endregion
}
