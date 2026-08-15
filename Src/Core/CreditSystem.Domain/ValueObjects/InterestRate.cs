using System.Text.Json.Serialization;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Exceptions;

namespace CreditSystem.Domain.ValueObjects;

public record InterestRate
{
    public decimal AnnualRate { get; init; }
    public RateType RateType { get; init; } = RateType.Fixed;
    public decimal Spread { get; init; } = 0;
    public string? ReferenceRateId { get; init; } = null;

    [JsonConstructor]
    public InterestRate()
    {
        AnnualRate = 0;
        RateType = RateType.Fixed;
        Spread = 0;
        ReferenceRateId = null;
    }

    public InterestRate(decimal annualRate)
    {
        if (annualRate < 0 || annualRate > 100)
            throw new DomainException("Invalid interest rate");
        AnnualRate = annualRate;
        RateType = RateType.Fixed;
        Spread = 0;
        ReferenceRateId = null;
    }

    public InterestRate(decimal annualRate, RateType rateType, decimal spread, string? referenceRateId)
    {
        if (annualRate < 0 || annualRate > 100)
            throw new DomainException("Invalid interest rate");
        if (spread < 0)
            throw new DomainException("Spread cannot be negative");
        AnnualRate = annualRate;
        RateType = rateType;
        Spread = spread;
        ReferenceRateId = referenceRateId;
    }

    public decimal MonthlyRate => AnnualRate / 12 / 100;
    public decimal DailyRate => AnnualRate / 365 / 100;

    public Money CalculateMonthlyInterest(Money principal)
        => new(principal.Amount * MonthlyRate, principal.Currency);

    public Money CalculateDailyInterest(Money principal)
        => new(principal.Amount * DailyRate, principal.Currency);
}