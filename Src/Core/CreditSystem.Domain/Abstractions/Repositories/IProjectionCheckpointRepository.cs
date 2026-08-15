namespace CreditSystem.Domain.Abstractions.Repositories;

public interface IProjectionCheckpointRepository
{
    /// <summary>Returns the last processed sequence for the given projector. Returns 0 if no checkpoint exists.</summary>
    Task<long> GetCheckpointAsync(string projectorName, CancellationToken ct = default);

    Task SaveCheckpointAsync(string projectorName, long sequence, CancellationToken ct = default);
}
