namespace SmartCore.Telemetry;

public class TelemetryOptions
{
    public string ServiceName { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string Environment { get; set; } = "development";

    // Endpoint Jaeger hoy / Collector mañana
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";

    public bool EnableRedis { get; set; } = false;
    public bool EnableMassTransit { get; set; } = false;
    public double SamplerRatio { get; set; } = 1.0; // default: 100% traces
}

