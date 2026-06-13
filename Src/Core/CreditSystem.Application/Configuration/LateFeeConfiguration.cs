namespace CreditSystem.Application.Configuration;

public class LateFeeConfiguration
{
    public decimal PercentageOfPayment { get; set; } = 5.0m;  // 5%
    public decimal FixedAmount { get; set; } = 25.0m;         // $25 mínimo
    public decimal DailyAmount { get; set; } = 1.0m;          // $1 por día
    public decimal MaximumFee { get; set; } = 100.0m;         // Tope $100
    public int GracePeriodDays { get; set; } = 5;             // 5 días de gracia
}