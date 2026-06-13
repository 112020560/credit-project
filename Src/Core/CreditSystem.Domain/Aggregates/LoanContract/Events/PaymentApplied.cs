using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.LoanContract.Events;

public record PaymentApplied : DomainEvent
{
    public Guid PaymentId { get; init; }
    public Money TotalAmount { get; init; }
    public Money PrincipalPaid { get; init; }
    public Money InterestPaid { get; init; }
    public Money FeePaid { get; init; }
    public Money NewBalance { get; init; }
    public int PaymentNumber { get; init; }
    public PaymentMethod Method { get; init; }
}