using CreditSystem.Application.Configuration;
using CreditSystem.Domain.Abstractions;
using CreditSystem.Domain.Abstractions.EventStore;
using CreditSystem.Domain.Abstractions.Persistence;
using CreditSystem.Domain.Abstractions.Projections;
using CreditSystem.Domain.Abstractions.Repositories;
using CreditSystem.Domain.Abstractions.Services;
using CreditSystem.Infrastructure.EventStore;
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

        services.AddScoped<IProjectionEngine,ProjectionEngine>();
        
        services.AddScoped<IRevolvingCreditRepository, RevolvingCreditRepository>();
        services.AddScoped<IRevolvingCreditQueryService>(sp => 
            new RevolvingCreditQueryService(connectionString));
        services.AddScoped<IProjection, RevolvingCreditSummaryProjector>();
        services.AddScoped<IProjection, PaymentTrackingProjector>();

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
        services.AddScoped<ILoanQueryService>(sp =>
            new LoanQueryService(
                configuration.GetConnectionString("CreditDb")!,
                sp.GetRequiredService<IOptions<LateFeeConfiguration>>()));
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

        return services;
    }

    private static IServiceCollection AddConsumerConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICustomerService>(sp =>
            new CustomerService(configuration.GetConnectionString("CreditDb")!));
        
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