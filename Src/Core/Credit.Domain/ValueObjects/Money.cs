using System;

namespace Credit.Domain.ValueObjects;

public sealed record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0, currency);

    public static Money operator +(Money a, Money b)
        => a.Currency == b.Currency
            ? new Money(a.Amount + b.Amount, a.Currency)
            : throw new InvalidOperationException("Currency mismatch");

    public static Money operator -(Money a, Money b)
        => a.Currency == b.Currency
            ? new Money(a.Amount - b.Amount, a.Currency)
            : throw new InvalidOperationException("Currency mismatch");

    public bool IsZero => Amount == 0;
}