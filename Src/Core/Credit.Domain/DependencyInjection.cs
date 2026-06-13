using System;
using Credit.Domain.Services.AmortizationStrategy;
using Microsoft.Extensions.DependencyInjection;

namespace Credit.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services) =>
        services.ConfigureServices();

    private static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        services.AddSingleton<IAmortizationStrategy, FrenchAmortizationStrategy>();
        services.AddSingleton<IAmortizationStrategy, GermanAmortizationStrategy>();
        services.AddSingleton<IAmortizationStrategy, AmericanAmortizationStrategy>();

        services.AddSingleton<AmortizationScheduleService>();
        return services;
    }
}
