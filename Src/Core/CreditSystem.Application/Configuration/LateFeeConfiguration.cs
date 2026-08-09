namespace CreditSystem.Application.Configuration;

public class LateFeeConfiguration
{
    public decimal PercentageOfPayment { get; set; } = 5.0m;  // 5%
    public decimal FixedAmount { get; set; } = 25.0m;         // $25 minimum
    public decimal DailyAmount { get; set; } = 1.0m;          // $1 per day
    public decimal MaximumFee { get; set; } = 100.0m;         // $100 cap
    // GracePeriodDays moved to UnderwritingPolicy.GracePeriodDays
}