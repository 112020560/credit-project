using System;

namespace Credit.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}