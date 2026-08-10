using CreditSystem.Application.Job;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Infrastructure.Locking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Infrastructure.Workers;

public class RevolvingInterestAccrualWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDistributedLock _distributedLock;
    private readonly ILogger<RevolvingInterestAccrualWorker> _logger;
    private readonly TimeSpan _runTime;

    public RevolvingInterestAccrualWorker(
        IServiceScopeFactory scopeFactory,
        IDistributedLock distributedLock,
        ILogger<RevolvingInterestAccrualWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _distributedLock = distributedLock;
        _logger = logger;
        _runTime = TimeSpan.Parse(
            configuration.GetValue<string>("Jobs:RevolvingInterestAccrual:RunTime", "02:30:00"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Revolving interest accrual worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = GetNextRunTime(now);
                var delay = nextRun - now;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next revolving interest accrual scheduled for {NextRun}", nextRun);
                    await Task.Delay(delay, stoppingToken);
                }

                var acquired = await _distributedLock.TryAcquireAsync(WorkerLockId.RevolvingInterestAccrual, stoppingToken);
                if (!acquired)
                {
                    _logger.LogDebug("Revolving interest accrual lock not available — another instance is running");
                    continue;
                }

                try
                {
                    await RunJobAsync(stoppingToken);
                }
                finally
                {
                    await _distributedLock.ReleaseAsync(WorkerLockId.RevolvingInterestAccrual, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in revolving interest accrual worker");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Revolving interest accrual worker stopped");
    }

    private async Task RunJobAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<IRevolvingInterestAccrualJob>();
        await job.ExecuteAsync(cancellationToken);
    }

    private DateTime GetNextRunTime(DateTime now)
    {
        var todayRun = now.Date.Add(_runTime);
        return now < todayRun ? todayRun : todayRun.AddDays(1);
    }
}
