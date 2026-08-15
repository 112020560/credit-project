using CreditSystem.Domain.Abstractions.Events;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.LoanContract.Events;

public record LoanDisbursed : DomainEvent
{
    public Money Amount { get; init; }
    public string DisbursementMethod { get; init; }  // WIRE, ACH, CHECK
    public string DestinationAccount { get; init; }
    public DateTime DisbursedAt { get; init; }
}
