using Credit.Application.Abstractions.Persistence;
using Credit.Domain.Enums;
using Credit.Domain.Services.AmortizationStrategy;
using Credit.Infrastructure.Adapters.Outbound.EntityFramework.Context;
using Credit.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;
using Credit.Infrastructure.Messaging.Consumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Credit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
        => services
            .AddPersistence(configuration)
            .AddDomainServices()
            .AddMessaging(configuration);

    private static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<CreditDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ICreditCustomerRepository, CreditCustomerRepository>();
        services.AddScoped<ICreditLineRepository, CreditLineRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    private static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddSingleton<IAmortizationStrategy, FrenchAmortizationStrategy>();
        services.AddSingleton<IAmortizationStrategy, GermanAmortizationStrategy>();
        services.AddSingleton<IAmortizationStrategy, AmericanAmortizationStrategy>();
        services.AddSingleton<AmortizationScheduleService>();

        return services;
    }

    private static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<SaleInvoiceConfirmedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration["RabbitMqSettings:Uri"]);

                cfg.ReceiveEndpoint("credit-sale-invoice-confirmed", e =>
                {
                    e.ConfigureConsumer<SaleInvoiceConfirmedConsumer>(context);
                });
            });
        });

        return services;
    }
}
