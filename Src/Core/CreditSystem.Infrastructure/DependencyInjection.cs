using CreditSystem.Application.Configuration;
using CreditSystem.Infrastructure.Members;
using CreditSystem.Application.Job;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Documents;
using CreditSystem.Domain.Abstractions.EventStore;
using CreditSystem.Domain.Abstractions.Persistence;
using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Infrastructure.Documents;

using CreditSystem.Infrastructure.EventStore;
using CreditSystem.Infrastructure.HealthChecks;
using CreditSystem.Infrastructure.Locking;
using CreditSystem.Infrastructure.Messaging.Outbox;
using CreditSystem.Infrastructure.Messaging.RabbitMq.Consumers;
using CreditSystem.Infrastructure.Messaging.RabbitMq.Messages;
using CreditSystem.Infrastructure.Projections;
using CreditSystem.Infrastructure.Projectors;
using CreditSystem.Infrastructure.Repositories;
using CreditSystem.Infrastructure.Services;
using CreditSystem.Infrastructure.Webhooks;
using CreditSystem.Infrastructure.Workers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddEventStore(configuration)
            .AddProjectorStore(configuration)
            .AddConsumerConfiguration(configuration)
            .AddPersistenceService(configuration)
            .AddPaymentInfrastructure(configuration)
            .AddWebhookInfrastructure(configuration)
            .AddWorkerConfiguration(configuration);
    
    private static IServiceCollection AddEventStore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDistributedLock>(_ =>
            new PostgresAdvisoryLock(configuration.GetConnectionString("CreditDb")!));

        services.AddSingleton<IEventSerializer, JsonEventSerializer>();
        services.AddSingleton<IHashGenerator, Sha256HashGenerator>();
        services.AddScoped<IEventStore>(sp => 
            new PostgresEventStore(
                configuration.GetConnectionString("CreditDb")!, //EventStore
                sp.GetRequiredService<IEventSerializer>(),
                sp.GetRequiredService<IHashGenerator>(),
                sp.GetRequiredService<ILogger<PostgresEventStore>>()));
        services.AddScoped<ILoanContractRepository, LoanContractRepository>();
        
        
        return services;
    }

    private static IServiceCollection AddProjectorStore(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CreditDb")!;
        services.AddSingleton<IProjectionStore>(sp =>
            new PostgresProjectionStore(connectionString, //readdb
                sp.GetRequiredService<ILogger<PostgresProjectionStore>>()));
        
        services.AddScoped<IProjection, LoanSummaryProjector>();
        services.AddScoped<IProjection, DelinquentLoansProjector>();
        services.AddScoped<IProjection, PaymentHistoryProjector>();
        services.AddScoped<IProjection, LoanPortfolioProjector>();
        services.AddScoped<IProjection, PendingDisbursementsProjector>();

        services.AddScoped<IProjectionEngine,ProjectionEngine>();
        
        services.AddScoped<IRevolvingCreditRepository, RevolvingCreditRepository>();
        services.AddScoped<IRevolvingCreditQueryService>(sp => 
            new RevolvingCreditQueryService(connectionString));
        services.AddScoped<IProjection, RevolvingCreditSummaryProjector>();
        services.AddScoped<IProjection, PaymentTrackingProjector>();
        services.AddScoped<IProjection, SocialCapitalProjector>();

        return services;
    }

    private static IServiceCollection AddPaymentInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Payment tracking repository
        services.AddScoped<IPaymentTrackingRepository, PaymentTrackingRepository>();

        // Outbox pattern
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        return services;
    }

    private static IServiceCollection AddWebhookInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Webhook repositories
        services.AddScoped<IWebhookSubscriptionRepository, WebhookSubscriptionRepository>();
        services.AddScoped<IWebhookDeliveryRepository, WebhookDeliveryRepository>();

        // Webhook notifier service
        services.AddScoped<IWebhookNotifier, WebhookNotifier>();

        // HTTP client for webhook delivery
        services.AddHttpClient("WebhookClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    private static IServiceCollection AddPersistenceService(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CreditDb")!;

        services.AddScoped<IUnderwritingPolicyRepository>(_ =>
            new UnderwritingPolicyRepository(connectionString));

        services.AddScoped<ILoanQueryService>(sp =>
            new LoanQueryService(
                connectionString,
                sp.GetRequiredService<IOptions<LateFeeConfiguration>>(),
                sp.GetRequiredService<IUnderwritingPolicyRepository>()));

        services.AddScoped<ICreditProductRepository>(sp =>
            new CreditProductRepository(connectionString));

        services.AddScoped<ILoanGuaranteeRepository>(sp =>
            new LoanGuaranteeRepository(connectionString));

        services.AddScoped<IRiskClassificationRepository>(sp =>
            new RiskClassificationRepository(connectionString));

        services.AddScoped<IIdempotencyRepository>(sp =>
            new IdempotencyRepository(connectionString));

        services.AddScoped<IAuditLogRepository>(sp =>
            new AuditLogRepository(
                connectionString,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AuditLogRepository>>()));

        services.AddScoped<IReferenceRateRepository, ReferenceRateRepository>();

        services.AddScoped<IProjectionCheckpointRepository>(_ =>
            new ProjectionCheckpointRepository(connectionString));
        services.AddScoped<IProjectionFailureRepository>(_ =>
            new ProjectionFailureRepository(connectionString));

        services.AddScoped<IRateAdjustmentJob, RateAdjustmentJob>();

        services.AddScoped<ScribanTemplateEngine>();
        services.AddScoped<QuestPdfRenderer>();
        services.AddScoped<ExcelExporter>();
        services.AddScoped<IDocumentGenerator, DocumentGenerator>();

        return services;
    }

    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        services.AddTransient<UnderwritingPolicyHealthCheck>();
        services.AddTransient<RabbitMqHealthCheck>();
        services.AddTransient<ProjectionHealthCheck>();
        return services;
    }

    private static IServiceCollection AddWorkerConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Existing workers
        services.AddHostedService<InterestAccrualWorker>();
        services.AddHostedService<PaymentMissedWorker>();
        services.AddHostedService<RevolvingInterestAccrualWorker>();
        services.AddHostedService<StatementGenerationWorker>();
        services.AddHostedService<RevolvingPaymentMissedWorker>();

        // Async payment workers
        services.AddHostedService<OutboxPublisherWorker>();
        services.AddHostedService<WebhookDeliveryWorker>();

        // Risk classification worker
        services.AddHostedService<RiskClassificationWorker>();

        // Rate adjustment worker
        services.AddHostedService<RateAdjustmentWorker>();

        // Async projection dispatcher
        services.Configure<ProjectionDispatcherOptions>(configuration.GetSection("ProjectionDispatcher"));
        services.AddHostedService<ProjectionDispatcherWorker>();

        return services;
    }

    private static IServiceCollection AddConsumerConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICustomerReadRepository>(sp =>
            new CustomerReadRepository(configuration.GetConnectionString("CreditDb")!));
        services.AddScoped<ICustomerCreditProfileRepository, CustomerCreditProfileRepository>();
        services.AddScoped<ICooperativeMemberRepository>(sp =>
            new CooperativeMemberRepository(configuration.GetConnectionString("CreditDb")!));

        services.Configure<MemberNumberFormatOptions>(
            configuration.GetSection(MemberNumberFormatOptions.SectionName));
        services.AddScoped<IMemberNumberGenerator>(sp =>
            new DefaultMemberNumberGenerator(
                configuration.GetConnectionString("CreditDb")!,
                sp.GetRequiredService<IOptions<MemberNumberFormatOptions>>()));
        
        services.AddMassTransit(x =>
        {
            // Customer event consumers
            x.AddConsumer<CustomerCreatedConsumer>();
            x.AddConsumer<CustomerUpdatedConsumer>();

            // Async payment consumers
            x.AddConsumer<ProcessPaymentConsumer>();
            x.AddConsumer<ProcessRevolvingPaymentConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration["RabbitMqSettings:Uri"]);

                // Customer events endpoint
                cfg.ReceiveEndpoint("credit-service-customer-events", e =>
                {
                    e.ConfigureConsumer<CustomerCreatedConsumer>(context);
                    e.ConfigureConsumer<CustomerUpdatedConsumer>(context);
                });

                // Async payment processing endpoint
                cfg.ReceiveEndpoint("credit-service-payments", e =>
                {
                    e.PrefetchCount = 16;
                    e.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(15),
                        TimeSpan.FromSeconds(30)));

                    e.ConfigureConsumer<ProcessPaymentConsumer>(context);
                    e.ConfigureConsumer<ProcessRevolvingPaymentConsumer>(context);
                });
            });
        });

        return services;
    }
}