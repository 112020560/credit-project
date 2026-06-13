using System.Text.Json.Serialization;
using CreditSystem.Domain.Exceptions;

namespace CreditSystem.Domain.ValueObjects;

// Domain/ValueObjects/Money.cs
public record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    
    [JsonConstructor]
    public Money()
    {
        Amount = 0;
        Currency = "USD";
    }

    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new DomainException("Money cannot be negative");

        Amount = Math.Round(amount, 2);
        Currency = currency;
    }

    public static Money Zero(string currency = "USD") => new(0, currency);
    
    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        if (other.Amount > Amount)
            throw new DomainException("Insufficient funds");
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Percentage(decimal percent) 
        => new(Amount * (percent / 100), Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Currency mismatch: {Currency} vs {other.Currency}");
    }

    public static Money operator +(Money a, Money b) => a.Add(b);
    public static Money operator -(Money a, Money b) => a.Subtract(b);
    public static bool operator >(Money a, Money b) => a.Amount > b.Amount;
    public static bool operator <(Money a, Money b) => a.Amount < b.Amount;
}