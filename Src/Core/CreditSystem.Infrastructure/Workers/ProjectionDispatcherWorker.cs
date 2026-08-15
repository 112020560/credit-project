using CreditSystem.Domain.Abstractions.EventStore;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Infrastructure.EventStore;
using CreditSystem.Infrastructure.Projectors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditSystem.Infrastructure.Workers;

public class ProjectionDispatcherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventSerializer _eventSerializer;
    private readonly ProjectionDispatcherOptions _options;
    private readonly ILogger<ProjectionDispatcherWorker> _logger;

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4)
    ];

    public ProjectionDispatcherWorker(
        IServiceScopeFactory scopeFactory,
        IEventSerializer eventSerializer,
        IOptions<ProjectionDispatcherOptions> options,
        ILogger<ProjectionDispatcherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _eventSerializer = eventSerializer;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProjectionDispatcherWorker started (interval: {Interval}s, batch: {Batch})",
            _options.IntervalSeconds, _options.BatchSize);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ProjectionDispatcherWorker tick");
            }
        }

        _logger.LogInformation("ProjectionDispatcherWorker stopped");
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var eventStore = sp.GetRequiredService<IEventStore>();
        var checkpoints = sp.GetRequiredService<IProjectionCheckpointRepository>();
        var failures = sp.GetRequiredService<IProjectionFailureRepository>();
        var projectors = sp.GetServices<IProjection>().ToList();

        if (projectors.Count == 0)
            return;

        // Each projector maintains its own checkpoint to advance independently.
        foreach (var projector in projectors)
        {
            try
            {
                await ProcessProjectorAsync(projector, eventStore, checkpoints, failures, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing projector {Projector}", projector.ProjectionName);
            }
        }
    }

    private async Task ProcessProjectorAsync(
        IProjection projector,
        IEventStore eventStore,
        IProjectionCheckpointRepository checkpoints,
        IProjectionFailureRepository failures,
        CancellationToken ct)
    {
        var lastSequence = await checkpoints.GetCheckpointAsync(projector.ProjectionName, ct);

        var batch = (await eventStore.GetEventsSinceSequenceAsync(lastSequence, _options.BatchSize, ct)).ToList();

        if (batch.Count == 0)
        {
            _logger.LogDebug("Projector {Projector}: no new events (checkpoint: {Checkpoint})",
                projector.ProjectionName, lastSequence);
            return;
        }

        _logger.LogInformation(
            "Projector {Projector}: processing {Count} event(s) (seq {From} → {To})",
            projector.ProjectionName, batch.Count, batch[0].Sequence, batch[^1].Sequence);

        foreach (var row in batch)
        {
            if (string.IsNullOrEmpty(row.EventType))
            {
                _logger.LogWarning(
                    "Projector {Projector}: event {EventId} (seq {Sequence}) has null event_type — skipping",
                    projector.ProjectionName, row.Id, row.Sequence);
                await checkpoints.SaveCheckpointAsync(projector.ProjectionName, row.Sequence, ct);
                continue;
            }

            _logger.LogInformation(
                "Projector {Projector}: dispatching {EventType} (seq {Sequence}, stream {StreamId})",
                projector.ProjectionName, row.EventType, row.Sequence, row.StreamId);

            bool succeeded = false;
            Exception? lastException = null;
            int attempts = 0;

            // Try up to RetryDelays.Length + 1 times (first attempt + retries)
            for (int attempt = 0; attempt <= RetryDelays.Length; attempt++)
            {
                if (attempt > 0)
                {
                    await Task.Delay(RetryDelays[attempt - 1], ct);
                }

                try
                {
                    var domainEvent = _eventSerializer.Deserialize(row.EventType, row.EventData);
                    await projector.ProjectAsync(domainEvent, ct);
                    succeeded = true;
                    attempts = attempt + 1;
                    break;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempts = attempt + 1;
                    _logger.LogWarning(ex,
                        "Projector {Projector}: {EventType} (seq {Sequence}) failed on attempt {Attempt}/{Max}",
                        projector.ProjectionName, row.EventType, row.Sequence, attempt + 1, RetryDelays.Length + 1);
                }
            }

            if (succeeded)
            {
                _logger.LogInformation(
                    "Projector {Projector}: {EventType} (seq {Sequence}) → OK (attempt {Attempts})",
                    projector.ProjectionName, row.EventType, row.Sequence, attempts);
            }
            else
            {
                _logger.LogError(lastException,
                    "Projector {Projector}: {EventType} (seq {Sequence}) permanently failed after {Attempts} attempts — recording failure",
                    projector.ProjectionName, row.EventType, row.Sequence, attempts);

                await failures.RecordFailureAsync(
                    row.Id,
                    row.StreamId,
                    row.EventType,
                    projector.ProjectionName,
                    lastException!.Message,
                    attempts,
                    ct);
            }

            // Always advance checkpoint, even after permanent failure.
            await checkpoints.SaveCheckpointAsync(projector.ProjectionName, row.Sequence, ct);
        }

        _logger.LogInformation(
            "Projector {Projector}: batch complete, checkpoint advanced to {Sequence}",
            projector.ProjectionName, batch[^1].Sequence);
    }
}
