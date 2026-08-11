using CreditSystem.Domain.Enums;

namespace CreditSystem.Domain.ValueObjects;

public record PaymentWaterfallStep(int Priority, PaymentComponent Component)
    : IComparable<PaymentWaterfallStep>
{
    public int CompareTo(PaymentWaterfallStep? other)
        => other is null ? 1 : Priority.CompareTo(other.Priority);
}
