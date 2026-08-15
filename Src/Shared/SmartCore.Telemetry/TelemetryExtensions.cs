using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SmartCore.Telemetry;

public static class TelemetryExtensions
{
    public static IServiceCollection AddSmartCoreTelemetry(
        this IServiceCollection services,
        Action<TelemetryOptions> configure)
    {
        var options = new TelemetryOptions();
        configure(options);

        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource
                    .AddService(options.ServiceName,
                        serviceVersion: options.Version)
                    .AddAttributes(new[]
                    {
                        new KeyValuePair<string, object>("environment", options.Environment)
                    });
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource("MassTransit");

                if (options.EnableRedis)
                    tracing.AddRedisInstrumentation();

                tracing.AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = new Uri(options.OtlpEndpoint);
                    otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                });

                tracing.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(options.SamplerRatio)));

            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("MassTransit")
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddMeter("Microsoft.AspNetCore.Server.Kestrel");

                metrics.AddOtlpExporter(otlp =>
                {
                    otlp.Endpoint = new Uri(options.OtlpEndpoint);
                });
            });

        return services;
    }
}
