using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Models;
using CreditSystem.Infrastructure.HealthChecks;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CreditSystem.Tests.Infrastructure.HealthChecks;

public class UnderwritingPolicyHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_PolicyLoaded_ReturnsHealthy()
    {
        var policy = new UnderwritingPolicy(8m, 90, NoScoreBehavior.ApproveWithPenalty,
            GracePeriodDays: 5, PenaltyRate: 0m, OriginationFeeRate: 0m);

        var check = new UnderwritingPolicyHealthCheck(policy);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("underwriting-policy", check, null, null)
        };

        var result = await check.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_PolicyNull_ReturnsUnhealthy()
    {
        var check = new UnderwritingPolicyHealthCheck(null);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("underwriting-policy", check, null, null)
        };

        var result = await check.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
