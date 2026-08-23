using Asp.Versioning;
using Crm.Application.ExternalCustomers;
using Crm.Application.ExternalCustomers.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.ExternalCustomers;

public class Register : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/external-customers", async (
            IMediator mediator,
            RegisterExternalCustomerDto request,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new RegisterExternalCustomerCommand(request), cancellationToken);
            return result.Match(
                () => Results.Ok(),
                errors => Results.BadRequest(errors));
        })
        .WithName("RegisterExternalCustomer")
        .WithTags("ExternalCustomers")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
