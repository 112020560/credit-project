using CreditSystem.Domain.Enums;

namespace CreditSystem.Domain.ValueObjects;

public record SocialCapitalConfig
{
    public SocialCapitalCalculationType CalculationType { get; init; }
    public decimal Value { get; init; }
    public SocialCapitalCollectionMode CollectionMode { get; init; }

    public Money Calculate(
        Money paymentAmount,
        Money principalPaid,
        Money originalAmount,
        string currency)
    {
        var amount = CalculationType switch
        {
            SocialCapitalCalculationType.FixedAmount
                => Value,
            SocialCapitalCalculationType.PercentageOfPayment
                => paymentAmount.Amount * Value / 100m,
            SocialCapitalCalculationType.PercentageOfPrincipalPaid
                => principalPaid.Amount * Value / 100m,
            SocialCapitalCalculationType.PercentageOfOriginalAmount
                => originalAmount.Amount * Value / 100m,
            _ => 0m
        };

        return new Money(Math.Round(amount, 2), currency);
    }
}
