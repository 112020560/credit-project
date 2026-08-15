using CreditSystem.Domain.Abstractions.Repositories;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CreditSystem.Infrastructure.HealthChecks;

public class ProjectionHealthCheck : IHealthCheck
{
    private readonly IProjectionFailureRepository _failures;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(30);

    public ProjectionHealthCheck(IProjectionFailureRepository failures)
    {
        _failures = failures;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _failures.CountRecentUnresolvedAsync(Window, cancellationToken);
            return count > 0
                ? HealthCheckResult.Degraded($"{count} unresolved projection failures in the last 30 minutes")
                : HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to query projection failures", ex);
        }
    }
}
