using Asp.Versioning;
using Credit.Application.UseCases.CreditLine;
using Credit.WebApi.Extensions;
using Credit.WebApi.Infrastructure;
using MediatR;

namespace Credit.WebApi.Endpoints.Credit;

public class Customers : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/credit/customers/{externalCustomerId}/credit-status",
            async (Guid externalCustomerId, IMediator mediator, CancellationToken token) =>
            {
                var result = await mediator.Send(
                    new GetCustomerCreditStatusQuery(externalCustomerId), token);

                return result.Match(
                    onSuccess: status => Results.Ok(status),
                    onFailure: error => CustomResults.Problem(error));
            })
            .WithName("GetCustomerCreditStatus")
            .WithTags("Credit Customers")
            .Produces<CustomerCreditStatusResponse>(StatusCodes.Status200OK)
            .WithOpenApi()
            .MapToApiVersion(new ApiVersion(1));
    }
}
