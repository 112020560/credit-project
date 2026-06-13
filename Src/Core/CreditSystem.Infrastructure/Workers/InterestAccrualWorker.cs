using CreditSystem.Application.Job;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CreditSystem.Infrastructure.Workers;

public class InterestAccrualWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InterestAccrualWorker> _logger;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _runTime;

    public InterestAccrualWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<InterestAccrualWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        
        // Configuración: cada cuánto revisar y a qué hora correr
        _interval = TimeSpan.FromHours(configuration.GetValue<int>("Jobs:InterestAccrual:IntervalHours", 1));
        _runTime = TimeSpan.Parse(configuration.GetValue<string>("Jobs:InterestAccrual:RunTime", "02:00:00"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Interest accrual worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = GetNextRunTime(now);
                var delay = nextRun - now;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next interest accrual scheduled for {NextRun}", nextRun);
                    await Task.Delay(delay, stoppingToken);
                }

                await RunJobAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutdown solicitado
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in interest accrual worker");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Interest accrual worker stopped");
    }

    private async Task RunJobAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<IInterestAccrualJob>();
        
        await job.ExecuteAsync(cancellationToken);
    }

    private DateTime GetNextRunTime(DateTime now)
    {
        var todayRun = now.Date.Add(_runTime);
        
        return now < todayRun ? todayRun : todayRun.AddDays(1);
    }
}