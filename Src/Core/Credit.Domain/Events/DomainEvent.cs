using System;

namespace Credit.Domain.Events;

public abstract record DomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

public record CreditCreated(Guid CreditId, CreditTerms Terms) : DomainEvent;
public record CreditApproved(Guid CreditId) : DomainEvent;
public record CreditFunded(Guid CreditId, decimal FundedAmount, DateTime FundedOn) : DomainEvent;
public record PaymentRegistered(Guid CreditId, Guid PaymentId, decimal Amount, DateTime PaidOn) : DomainEvent;
public record PaymentAppliedToInstallment(Guid CreditId, Guid PaymentId, int InstallmentNumber, decimal PrincipalApplied, decimal InterestApplied) : DomainEvent;
public record CreditMarkedLate(Guid CreditId, DateTime MarkedOn) : DomainEvent;
public record CreditClosed(Guid CreditId, DateTime ClosedOn) : DomainEvent;

