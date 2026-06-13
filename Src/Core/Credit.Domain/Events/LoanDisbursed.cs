using System;
using Credit.Domain.ValueObjects;

namespace Credit.Domain.Events;

public sealed record LoanDisbursed(
    CreditId CreditId,
    Money Amount,
    DateTime OccurredAt
) : IDomainEvent;
