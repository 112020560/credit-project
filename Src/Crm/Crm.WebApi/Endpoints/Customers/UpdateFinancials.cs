using Asp.Versioning;
using Crm.Application.Customers;
using Crm.Application.Customers.Dtos;
using Crm.WebApi.Extensions;
using MediatR;
using SharedKernel.Enums;

namespace Crm.WebApi.Endpoints.Customers;

public class UpdateFinancials : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("/customers/{customerId}/financials", async (
            Guid customerId,
            UpdateCustomerFinancialsDto request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new UpdateCustomerFinancialsCommand(customerId, request), cancellationToken);
            return result.Match(
                () => Results.NoContent(),
                error => error.Type == ErrorType.NotFound
                    ? Results.NotFound(error)
                    : Results.UnprocessableEntity(error));
        })
        .WithName("UpdateCustomerFinancials")
        .WithTags("Customers")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
