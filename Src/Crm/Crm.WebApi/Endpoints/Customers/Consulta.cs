using Asp.Versioning;
using Crm.Application.Customers;
using Crm.Application.Customers.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.Customers;

public class Consulta : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/customers/{customerId}", async (Guid customerId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCustomerByIdQuery(customerId));
            return result.Match(
                customer => Results.Ok(customer),
                _ => Results.NotFound());
        })
        .WithName("GetCustomerById")
        .WithTags("Customers")
        .Produces(200)
        .Produces(404)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));

        app.MapGet("/customers", async (
            IMediator mediator,
            string? q,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await mediator.Send(new SearchCustomersQuery(q, page, pageSize));
            return result.Match(
                response => Results.Ok(response),
                errors => Results.BadRequest(errors));
        })
        .WithName("SearchCustomers")
        .WithTags("Customers")
        .Produces<CustomerSearchPagedResponse>(200)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
