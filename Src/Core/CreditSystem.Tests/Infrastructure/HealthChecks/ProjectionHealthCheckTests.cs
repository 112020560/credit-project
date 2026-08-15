using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Infrastructure.HealthChecks;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;

namespace CreditSystem.Tests.Infrastructure.HealthChecks;

public class ProjectionHealthCheckTests
{
    private static HealthCheckContext MakeContext(IHealthCheck check) => new()
    {
        Registration = new HealthCheckRegistration("projection-health", check, null, null)
    };

    [Fact]
    public async Task CheckHealthAsync_NoRecentFailures_ReturnsHealthy()
    {
        var failures = Substitute.For<IProjectionFailureRepository>();
        failures.CountRecentUnresolvedAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(0);

        var check = new ProjectionHealthCheck(failures);
        var result = await check.CheckHealthAsync(MakeContext(check));

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithRecentFailures_ReturnsDegraded()
    {
        var failures = Substitute.For<IProjectionFailureRepository>();
        failures.CountRecentUnresolvedAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(3);

        var check = new ProjectionHealthCheck(failures);
        var result = await check.CheckHealthAsync(MakeContext(check));

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("3");
    }
}
