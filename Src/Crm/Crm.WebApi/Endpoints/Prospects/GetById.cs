using Asp.Versioning;
using Crm.Application.Prospects;
using Crm.Application.Prospects.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.Prospects;

public class ProspectsGetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/prospects/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetProspectByIdQuery(id), ct);
            return result.Match(
                dto => Results.Ok(dto),
                _ => Results.NotFound());
        })
        .WithName("GetProspectById")
        .WithTags("Prospects")
        .Produces<ProspectSummaryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
