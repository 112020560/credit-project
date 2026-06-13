using System;

namespace Credit.Domain.ValueObjects;

public readonly record struct CreditId(string Value)
{
    public static CreditId New()
        => new(Guid.NewGuid().ToString());
}
