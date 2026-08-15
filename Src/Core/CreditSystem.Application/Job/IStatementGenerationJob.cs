namespace CreditSystem.Application.Job;

public interface IStatementGenerationJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}