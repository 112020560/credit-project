using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Enums;
using CreditSystem.Domain.Models;
using CreditSystem.Infrastructure.HealthChecks;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;

namespace CreditSystem.Tests.Infrastructure.HealthChecks;

public class UnderwritingPolicyHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_PolicyLoaded_ReturnsHealthy()
    {
        var policy = new UnderwritingPolicy(8m, 90, NoScoreBehavior.ApproveWithPenalty,
            GracePeriodDays: 5, PenaltyRate: 0m, OriginationFeeRate: 0m);

        var repo = Substitute.For<IUnderwritingPolicyRepository>();
        repo.GetByIdAsync("default", Arg.Any<CancellationToken>()).Returns(policy);

        var check = new UnderwritingPolicyHealthCheck(repo);
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
        var repo = Substitute.For<IUnderwritingPolicyRepository>();
        repo.GetByIdAsync("default", Arg.Any<CancellationToken>()).Returns((UnderwritingPolicy?)null);

        var check = new UnderwritingPolicyHealthCheck(repo);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("underwriting-policy", check, null, null)
        };

        var result = await check.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
