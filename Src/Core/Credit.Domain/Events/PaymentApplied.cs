using System;
using Credit.Domain.ValueObjects;

namespace Credit.Domain.Events;

public sealed record PaymentApplied(
    CreditId CreditId,
    Money Amount,
    PaymentBreakdown Breakdown,
    DateTime OccurredAt
) : IDomainEvent;