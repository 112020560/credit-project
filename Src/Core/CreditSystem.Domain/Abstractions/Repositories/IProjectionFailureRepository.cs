using CreditSystem.Domain.Models;

namespace CreditSystem.Domain.Abstractions.Repositories;

public interface IProjectionFailureRepository
{
    Task RecordFailureAsync(
        Guid eventId,
        Guid streamId,
        string eventType,
        string projectorName,
        string errorMessage,
        int attempts,
        CancellationToken ct = default);

    /// <summary>Marks a failure as resolved. Returns false if the id was not found.</summary>
    Task<bool> ResolveAsync(Guid id, CancellationToken ct = default);

    Task<IEnumerable<ProjectionFailure>> GetUnresolvedAsync(CancellationToken ct = default);

    Task<IEnumerable<ProjectionFailure>> GetAllAsync(CancellationToken ct = default);

    Task<int> CountRecentUnresolvedAsync(TimeSpan window, CancellationToken ct = default);
}
