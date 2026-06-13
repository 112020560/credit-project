using Credit.Domain.ValueObjects;

namespace Credit.Domain.Events;

public sealed record CreditContractCreated(
    CreditId CreditId,
    string Currency,
    DateTime OccurredAt
) : IDomainEvent;