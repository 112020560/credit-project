namespace CreditSystem.Domain.ValueObjects;

public record ProductLimits
{
    public decimal MinAmount { get; init; }
    public decimal MaxAmount { get; init; }
    public int MinTermMonths { get; init; }
    public int MaxTermMonths { get; init; }

    public ProductLimits(decimal minAmount, decimal maxAmount, int minTermMonths, int maxTermMonths)
    {
        if (minAmount >= maxAmount)
            throw new ArgumentException("MinAmount must be less than MaxAmount");
        if (minTermMonths >= maxTermMonths)
            throw new ArgumentException("MinTermMonths must be less than MaxTermMonths");
        if (minAmount <= 0)
            throw new ArgumentException("MinAmount must be greater than zero");
        if (minTermMonths <= 0)
            throw new ArgumentException("MinTermMonths must be greater than zero");

        MinAmount = minAmount;
        MaxAmount = maxAmount;
        MinTermMonths = minTermMonths;
        MaxTermMonths = maxTermMonths;
    }
}
