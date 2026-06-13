using System;
using Credit.Domain.Events;

namespace Credit.Domain.Abstractions;

public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _events = new();

    protected void Raise(IDomainEvent @event)
    {
        _events.Add(@event);
        Apply(@event);
    }

    protected abstract void Apply(IDomainEvent @event);

    public IReadOnlyCollection<IDomainEvent> GetUncommittedEvents()
        => _events.AsReadOnly();
}
