using CreditSystem.Domain.Abstractions.Repositories;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CreditSystem.Infrastructure.HealthChecks;

public class UnderwritingPolicyHealthCheck : IHealthCheck
{
    private readonly IUnderwritingPolicyRepository _repository;

    public UnderwritingPolicyHealthCheck(IUnderwritingPolicyRepository repository)
    {
        _repository = repository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var policy = await _repository.GetByIdAsync("default", cancellationToken);
            return policy is not null
                ? HealthCheckResult.Healthy("UnderwritingPolicy 'default' is accessible")
                : HealthCheckResult.Unhealthy("UnderwritingPolicy 'default' not found in database");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to load UnderwritingPolicy", ex);
        }
    }
}
