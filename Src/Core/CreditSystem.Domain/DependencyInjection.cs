using CreditSystem.Domain.Rules;
using CreditSystem.Domain.Rules.Implementations;
using CreditSystem.Domain.Services.Amortization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CreditSystem.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddBusiness(
        this IServiceCollection services,
        IConfiguration configuration)
        => services
            .AddRulesEngine(configuration)
            .AddCalculateEngine(configuration);

    private static IServiceCollection AddRulesEngine(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IContractRule, MaxLoanAmountRule>();
        services.AddScoped<IContractRule, CreditScoreRule>();
        services.AddScoped<IContractRule, DebtToIncomeRule>();
        services.AddScoped<IContractRule, CollateralRule>();
        services.AddScoped<IContractRule, ActiveLoansRule>();
        services.AddScoped<ContractEngine>();

        return services;
    }
    
    private static IServiceCollection AddCalculateEngine(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Calculation Engine
        services.AddSingleton<IAmortizationCalculator, FrenchAmortizationCalculator>();
        services.AddSingleton<IAmortizationCalculator, GermanAmortizationCalculator>();
        services.AddSingleton<IAmortizationCalculator, AmericanAmortizationCalculator>();
        services.AddSingleton<IAmortizationCalculator, FlatAmortizationCalculator>();
        services.AddSingleton<IAmortizationCalculator, InterestOnlyAmortizationCalculator>();
        services.AddSingleton<IAmortizationCalculatorFactory, AmortizationCalculatorFactory>();
        return services;
    }
}