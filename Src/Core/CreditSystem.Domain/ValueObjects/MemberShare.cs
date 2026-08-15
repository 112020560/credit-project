namespace CreditSystem.Domain.ValueObjects;

public record MemberShare
{
    public Money TotalAmount { get; init; }
    public int NumberOfContributions { get; init; }
    public DateTime? LastContributionDate { get; init; }

    public MemberShare(Money totalAmount, int numberOfContributions, DateTime? lastContributionDate)
    {
        TotalAmount = totalAmount;
        NumberOfContributions = numberOfContributions;
        LastContributionDate = lastContributionDate;
    }

    public static MemberShare Zero(string currency = "CRC") =>
        new(Money.Zero(currency), 0, null);

    public MemberShare WithUpdatedContribution(Money additionalAmount, DateTime date) =>
        new(TotalAmount + additionalAmount, NumberOfContributions + 1, date);
}
