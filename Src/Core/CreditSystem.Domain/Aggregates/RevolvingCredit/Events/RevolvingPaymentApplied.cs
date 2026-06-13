using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.RevolvingCredit.Events;

public record RevolvingPaymentApplied : DomainEvent
{
    public Guid PaymentId { get; init; }
    public Money TotalAmount { get; init; } = null!;
    public Money InterestPaid { get; init; } = null!;
    public Money FeesPaid { get; init; } = null!;
    public Money PrincipalPaid { get; init; } = null!;
    public Money NewBalance { get; init; } = null!;
    public Money AvailableCredit { get; init; } = null!;
    public PaymentMethod Method { get; init; }
}