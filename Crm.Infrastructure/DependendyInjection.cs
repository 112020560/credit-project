using Crm.Application.Abstractions.Mq;
using Crm.Domain.Abstractions.Persistence;
using Crm.Infrastructure.Adapters.Outbound.EntityFramework;
using Crm.Infrastructure.Adapters.Outbound.EntityFramework.Repositories;
using Crm.Infrastructure.Adapters.Outbound.Messaging.RabbitMq;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Crm.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            //.ConfigureDataAccessService()
            .ConfigurePersistenceService(configuration)
            .AddHealthChecks(configuration)
            .AddProducerService(configuration)
            .AddHttpClientsServices(configuration);
    private static IServiceCollection ConfigurePersistenceService(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new Exception("No fue posible cargar el string de conexion");
        services.AddDbContext<CrmDbContext>(options =>
            options.UseNpgsql(connectionString)
                .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Debug)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
        );
        services.AddScoped<ICustomersRepository, CustomersRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("DefaultConnection")!);

        return services;
    }

    private static IServiceCollection AddProducerService(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitHost = configuration.GetSection("RabbitMqSettings:Uri").Value ?? Environment.GetEnvironmentVariable("RABBIT_HOST") ??
            throw new Exception("The rabbitMQ connection was not supled");
        
        services.AddMassTransit(x =>
        {
            
            x.UsingRabbitMq((context, config) => 
            {
                config.Host(rabbitHost);
            });
            
            //PARA MANEJAR REQUESTRESPONSE MESSAGE
            //x.AddRequestClient<ReqRespDocumentoContract>();
        });

        
        services.AddScoped<IMqProducerService, MqProducerService>();
        services.AddScoped(typeof(IMqRequestResponse<>), typeof(MqRequestResponse<>));
        return services;
    }

    private static IServiceCollection AddHttpClientsServices(this IServiceCollection services, IConfiguration configuration)
    {
        //services.AddHttpClient();
        //services.AddScoped<IHttpServices, HttpServices>();

        return services;
    }
}