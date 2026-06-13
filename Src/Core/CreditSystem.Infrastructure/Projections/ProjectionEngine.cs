using System.Runtime.CompilerServices;
using CreditSystem.Domain.Abstractions.EventStore;
using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Aggregates.LoanContract.Events.Base;
using CreditSystem.Infrastructure.EventStore;
using CreditSystem.Infrastructure.Projectors;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Infrastructure.Projections;

// Infrastructure/Projections/ProjectionEngine.cs
public class ProjectionEngine: IProjectionEngine
{
    private readonly IEventStore _eventStore;
    private readonly IProjectionStore _projectionStore;
    private readonly IEnumerable<IProjection> _projections;
    private readonly ILogger<ProjectionEngine> _logger;

    public ProjectionEngine(
        IEventStore eventStore,
        IProjectionStore projectionStore,
        IEnumerable<IProjection> projections,
        ILogger<ProjectionEngine> logger)
    {
        _eventStore = eventStore;
        _projectionStore = projectionStore;
        _projections = projections;
        _logger = logger;
    }

    public async Task ProjectEventAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        foreach (var projection in _projections)
        {
            try
            {
                await projection.ProjectAsync(@event, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error projecting event {EventType} to {Projection}",
                    @event.GetType().Name, projection.ProjectionName);
                throw;
            }
        }
    }

    public async Task ProjectEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        var eventList = events.ToList();

        if (eventList.Count == 0)
            return;

        _logger.LogDebug("Projecting {Count} events", eventList.Count);

        foreach (var @event in eventList)
        {
            await ProjectEventAsync(@event, ct);
        }
    }

    public async Task RebuildProjectionAsync(string projectionName, CancellationToken ct = default)
    {
        var projection = _projections.FirstOrDefault(p => p.ProjectionName == projectionName)
            ?? throw new InvalidOperationException($"Projection {projectionName} not found");

        _logger.LogInformation("Rebuilding projection {Projection}", projectionName);

        var events = GetAllEventsAsync(ct);
        await projection.RebuildAsync(events, ct);

        _logger.LogInformation("Finished rebuilding projection {Projection}", projectionName);
    }

    public async Task RebuildAllProjectionsAsync(CancellationToken ct = default)
    {
        foreach (var projection in _projections)
        {
            await RebuildProjectionAsync(projection.ProjectionName, ct);
        }
    }

    private async IAsyncEnumerable<IDomainEvent> GetAllEventsAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        var events = await _eventStore.GetAllEventsAsync(limit: 10000, ct: ct);
        
        foreach (var @event in events)
        {
            yield return @event;
        }
    }
}