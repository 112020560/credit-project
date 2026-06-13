namespace CreditSystem.Application.Job;

public interface IPaymentMissedJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}