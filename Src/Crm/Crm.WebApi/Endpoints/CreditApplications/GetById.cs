using Asp.Versioning;
using Crm.Application.CreditApplications;
using Crm.Application.CreditApplications.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.CreditApplications;

public class CreditApplicationsGetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/credit-applications/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCreditApplicationByIdQuery(id), ct);
            return result.Match(
                dto => Results.Ok(dto),
                _ => Results.NotFound());
        })
        .WithName("GetCreditApplicationById")
        .WithTags("CreditApplications")
        .Produces<CreditApplicationDetailDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
