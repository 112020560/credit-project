using System.Reflection;
using Asp.Versioning;
using Crm.Application;
using Crm.Infrastructure;
using Crm.WebApi;
using Crm.WebApi.Extensions;
using Serilog;
using SmartCore.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddSmartCoreTelemetry(options =>
{
    options.ServiceName = "crm-service";
    options.Version = "1.0.0";
    options.Environment = builder.Environment.EnvironmentName;

    options.OtlpEndpoint = builder.Configuration["Telemetry:OtlpEndpoint"] ?? "http://localhost:4317";

    // Instrumentaciones
    options.EnableMassTransit = true;
    options.EnableRedis = false;

    // 100% sampling (dev)
    options.SamplerRatio = 1.0;
});

builder.Services.AddSwaggerGen();

builder.Services
    .AddApplication()
    .AddPresentation()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

builder.Services.AddApiVersionExtension();

var app = builder.Build();

var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1))
    .ReportApiVersions()
    .Build();

var versionsGroup = app.MapGroup("api/v{version:apiVersion}")
    .WithApiVersionSet(apiVersionSet);
app.MapEndpoints(versionsGroup);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi();
}

app.MapHealthChecks("/api/health");

app.UseRequestContextLogging();

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

app.UseAuthentication();

app.UseAuthorization();

app.UseHttpsRedirection();

app.Run();