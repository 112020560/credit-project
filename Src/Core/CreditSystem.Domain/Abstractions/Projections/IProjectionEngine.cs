using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;

namespace CreditSystem.Domain.Abstractions.Projections;

public interface IProjectionEngine
{
    Task ProjectEventAsync(IDomainEvent @event, CancellationToken ct = default);

    /// <summary>
    /// Projects multiple events in sequence. Use this instead of looping over ProjectEventAsync.
    /// </summary>
    Task ProjectEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);

    Task RebuildProjectionAsync(string projectionName, CancellationToken ct = default);
    Task RebuildAllProjectionsAsync(CancellationToken ct = default);
}