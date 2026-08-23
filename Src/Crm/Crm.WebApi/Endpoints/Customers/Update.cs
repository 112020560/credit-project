using Asp.Versioning;
using Crm.Application.Customers;
using Crm.Application.Customers.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.Customers;

public class Update : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/customers/{customerId}", async (
            Guid customerId,
            UpdateCustomerDto request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new UpdateCustomerCommand(customerId, request), cancellationToken);
            return result.Match(
                customer => Results.Ok(customer),
                errors => Results.BadRequest(errors));
        })
        .WithName("UpdateCustomer")
        .WithTags("Customers")
        .Produces<CustomerSummaryDto>(200)
        .Produces(400)
        .Produces(404)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
