namespace CreditSystem.Application.Job;

public interface IRateAdjustmentJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
