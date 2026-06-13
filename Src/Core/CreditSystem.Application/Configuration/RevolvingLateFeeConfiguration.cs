namespace CreditSystem.Application.Configuration;

public class RevolvingLateFeeConfiguration
{
    public decimal PercentageOfMinimum { get; set; } = 5.0m;
    public decimal FixedAmount { get; set; } = 35.0m;
    public decimal DailyAmount { get; set; } = 1.0m;
    public decimal MaximumFee { get; set; } = 75.0m;
    public int DaysToFreeze { get; set; } = 30;
}