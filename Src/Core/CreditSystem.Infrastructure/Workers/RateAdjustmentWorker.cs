using CreditSystem.Application.Job;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Infrastructure.Locking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Infrastructure.Workers;

public class RateAdjustmentWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDistributedLock _distributedLock;
    private readonly ILogger<RateAdjustmentWorker> _logger;

    public RateAdjustmentWorker(
        IServiceScopeFactory scopeFactory,
        IDistributedLock distributedLock,
        ILogger<RateAdjustmentWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _distributedLock = distributedLock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Rate adjustment worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = GetNextFirstOfMonth(now);
                var delay = nextRun - now;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next rate adjustment scheduled for {NextRun}", nextRun);
                    await Task.Delay(delay, stoppingToken);
                }

                var acquired = await _distributedLock.TryAcquireAsync(WorkerLockId.RateAdjustment, stoppingToken);
                if (!acquired)
                {
                    _logger.LogDebug("Rate adjustment lock not available — another instance is running");
                    // Wait a bit before retrying so we don't spin
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                    continue;
                }

                try
                {
                    await RunJobAsync(stoppingToken);
                }
                finally
                {
                    await _distributedLock.ReleaseAsync(WorkerLockId.RateAdjustment, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in rate adjustment worker");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        _logger.LogInformation("Rate adjustment worker stopped");
    }

    private async Task RunJobAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<IRateAdjustmentJob>();
        await job.ExecuteAsync(cancellationToken);
    }

    private static DateTime GetNextFirstOfMonth(DateTime now)
    {
        var firstOfNextMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(1);
        return now.Day == 1 && now.Hour == 0 ? now : firstOfNextMonth;
    }
}
