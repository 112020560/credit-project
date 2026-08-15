namespace CreditSystem.Application.Job;

public interface IInterestAccrualJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}