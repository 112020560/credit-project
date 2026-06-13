using System;
using Credit.Domain.Events;

namespace Credit.Application.Tests;

public sealed class FakeEventStore
{
    public readonly List<IDomainEvent> Events = new();

    public void Save(IEnumerable<IDomainEvent> events)
    {
        Events.AddRange(events);
    }
}
