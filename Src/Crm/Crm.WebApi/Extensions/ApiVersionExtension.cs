using System;
using Asp.Versioning;

namespace Crm.WebApi.Extensions;

public static class ApiVersionExtension
{
    public static IServiceCollection AddApiVersionExtension(this IServiceCollection services)
    {
        services.AddApiVersioning(config =>
        {
            config.DefaultApiVersion = new(1);
            config.ReportApiVersions = true;
            config.AssumeDefaultVersionWhenUnspecified = true;
            config.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddApiExplorer(config =>
        {
            config.GroupNameFormat = "'v'VVV";
            config.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
