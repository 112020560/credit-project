using CreditSystem.Domain.Abstractions.Events;
using CreditSystem.Domain.ValueObjects;

namespace CreditSystem.Domain.Aggregates.LoanContract.Events;

public record RateAdjusted : DomainEvent
{
    public decimal OldRate { get; init; }
    public decimal NewRate { get; init; }
    public decimal Spread { get; init; }
    public string ReferenceRateId { get; init; } = null!;
    public decimal ReferenceRateValue { get; init; }
    public DateTime AdjustedAt { get; init; }
    public PaymentSchedule NewSchedule { get; init; } = null!;
}
