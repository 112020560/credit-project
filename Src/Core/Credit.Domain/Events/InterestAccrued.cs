using System;
using Credit.Domain.ValueObjects;

namespace Credit.Domain.Events;

public sealed record InterestAccrued(
    CreditId CreditId,
    DateRange Period,
    Money Amount,
    DateTime OccurredAt
) : IDomainEvent;