namespace CreditSystem.Application.Job;

public interface IRiskClassificationJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
