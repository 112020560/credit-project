using Asp.Versioning;
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

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

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
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Credit System API v1");
    });
}

app.UseExceptionHandler();

// Versioned endpoint group
var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

var v1 = app.MapGroup("/api/v{version:apiVersion}")
    .WithApiVersionSet(versionSet);

v1.MapLoanContractEndpoints();
v1.MapMemberEndpoints();
v1.MapProductEndpoints();
v1.MapGuaranteeEndpoints();
v1.MapAdminEndpoints();
v1.MapDelinquentLoansEndpoints();
v1.MapRevolvingCreditEndpoints();
v1.MapPaymentsEndpoints();
v1.MapWebhooksEndpoints();
v1.MapRiskEndpoints();

app.UseHttpsRedirection();



app.Run();