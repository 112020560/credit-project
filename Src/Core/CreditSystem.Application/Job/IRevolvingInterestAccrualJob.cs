namespace CreditSystem.Application.Job;

public interface IRevolvingInterestAccrualJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}