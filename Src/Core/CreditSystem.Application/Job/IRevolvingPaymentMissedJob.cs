namespace CreditSystem.Application.Job;

public interface IRevolvingPaymentMissedJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}