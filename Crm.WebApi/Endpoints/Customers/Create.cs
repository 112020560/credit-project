using Asp.Versioning;
using Crm.Application.Customers;
using Crm.Application.Customers.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.Customers;

public class Customers : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/customers", async (IMediator mediator, CreateCustomerDto request, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CreateCustomerCommand(request), cancellationToken);
            return result.Match(
                customerDto => Results.Created($"/customers/{customerDto.Id}", customerDto),
                errors => Results.BadRequest(errors));
        })
        .WithName("CreateCustomer")
        .WithTags("Customers")
        .Produces<CustomerSummaryDto>(StatusCodes.Status201Created)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}