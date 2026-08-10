using CreditSystem.Application.Job;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Infrastructure.Workers;

public class RiskClassificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RiskClassificationWorker> _logger;
    private readonly TimeSpan _runTime;

    public RiskClassificationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<RiskClassificationWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _runTime = TimeSpan.Parse(configuration.GetValue<string>("Jobs:RiskClassification:RunTime", "02:00:00"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Risk classification worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = GetNextRunTime(now);
                var delay = nextRun - now;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next risk classification scheduled for {NextRun}", nextRun);
                    await Task.Delay(delay, stoppingToken);
                }

                await RunJobAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in risk classification worker");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Risk classification worker stopped");
    }

    private async Task RunJobAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<IRiskClassificationJob>();
        await job.ExecuteAsync(cancellationToken);
    }

    private DateTime GetNextRunTime(DateTime now)
    {
        var todayRun = now.Date.Add(_runTime);
        return now < todayRun ? todayRun : todayRun.AddDays(1);
    }
}
