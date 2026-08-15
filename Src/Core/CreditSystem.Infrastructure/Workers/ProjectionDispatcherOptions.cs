namespace CreditSystem.Infrastructure.Workers;

public class ProjectionDispatcherOptions
{
    public int IntervalSeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 100;
}
