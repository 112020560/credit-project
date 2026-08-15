using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Sockets;

namespace CreditSystem.Infrastructure.HealthChecks;

public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly string _host;
    private readonly int _port;

    public RabbitMqHealthCheck(IConfiguration configuration)
    {
        var raw = configuration["RabbitMqSettings:Uri"] ?? "amqp://localhost:5672";
        var uri = new Uri(raw);
        _host = uri.Host;
        _port = uri.Port > 0 ? uri.Port : 5672;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port, cancellationToken);
            return HealthCheckResult.Healthy($"RabbitMQ reachable at {_host}:{_port}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"RabbitMQ not reachable at {_host}:{_port}", ex);
        }
    }
}
