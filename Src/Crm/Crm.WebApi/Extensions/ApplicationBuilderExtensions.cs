using System;

namespace Crm.WebApi.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSwaggerWithUi(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(opts =>
         {
             var description = app.DescribeApiVersions();
             foreach (var desc in description)
             {
                 var url = $"{desc.GroupName}/swagger.json";
                 var name = desc.GroupName.ToUpperInvariant();
                 opts.SwaggerEndpoint(url, name);
             }
         });

        return app;
    }
}
