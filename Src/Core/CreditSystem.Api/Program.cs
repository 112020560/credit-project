using CreditSystem.Api.Endpoints;
using CreditSystem.Api.EndPoints;
using CreditSystem.Api.Infrastructure;
using CreditSystem.Application;
using CreditSystem.Domain;
using CreditSystem.Infrastructure;
using Serilog;
using SmartCore.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddSmartCoreTelemetry(options =>
{
    options.ServiceName    = "credit-system-service";
    options.Version        = "1.0.0";
    options.Environment    = builder.Environment.EnvironmentName;
    options.OtlpEndpoint   = builder.Configuration["Telemetry:OtlpEndpoint"] ?? "http://localhost:4317";
    options.EnableMassTransit = true;
    options.SamplerRatio   = 1.0;
});

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddBusiness(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

//builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Credit System API", Version = "v1" });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

// Endpoints
app.MapLoanContractEndpoints();
app.MapAdminEndpoints();
app.MapDelinquentLoansEndpoints();
app.MapRevolvingCreditEndpoints();
app.MapPaymentsEndpoints();
app.MapWebhooksEndpoints();

app.UseHttpsRedirection();



app.Run();