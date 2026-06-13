using System;

namespace SmartCore.Credit.Domain;

public sealed class Payment
{
    public Guid Id { get; } = Guid.NewGuid();
    public decimal Amount { get; }
    public DateTime PaidOn { get; }

    public Payment(decimal amount, DateTime paidOn)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be > 0", nameof(amount));
        Amount = decimal.Round(amount, 2);
        PaidOn = paidOn;
    }
}
