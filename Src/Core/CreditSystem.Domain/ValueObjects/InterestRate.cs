using System.Text.Json.Serialization;
using CreditSystem.Domain.Exceptions;

namespace CreditSystem.Domain.ValueObjects;

public record InterestRate
{
    public decimal AnnualRate { get; init; }

    [JsonConstructor]
    public InterestRate()
    {
        AnnualRate = 0;
    }

    
    public InterestRate(decimal annualRate)
    {
        if (annualRate < 0 || annualRate > 100)
            throw new DomainException("Invalid interest rate");
        AnnualRate = annualRate;
    }

    public decimal MonthlyRate => AnnualRate / 12 / 100;
    public decimal DailyRate => AnnualRate / 365 / 100;

    public Money CalculateMonthlyInterest(Money principal)
        => new(principal.Amount * MonthlyRate, principal.Currency);

    public Money CalculateDailyInterest(Money principal)
        => new(principal.Amount * DailyRate, principal.Currency);
}