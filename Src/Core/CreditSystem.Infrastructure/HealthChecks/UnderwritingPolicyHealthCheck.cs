using CreditSystem.Domain.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CreditSystem.Infrastructure.HealthChecks;

public class UnderwritingPolicyHealthCheck : IHealthCheck
{
    private readonly UnderwritingPolicy? _policy;

    public UnderwritingPolicyHealthCheck(UnderwritingPolicy? policy = null)
    {
        _policy = policy;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return _policy is not null
            ? Task.FromResult(HealthCheckResult.Healthy("UnderwritingPolicy is loaded"))
            : Task.FromResult(HealthCheckResult.Unhealthy("UnderwritingPolicy is not loaded"));
    }
}
