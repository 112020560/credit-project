using Asp.Versioning;
using Crm.Application.Prospects;
using Crm.Application.Prospects.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.Prospects;

public class ProspectsEnrich : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/prospects/{id:guid}/enrich", async (Guid id, IMediator mediator, EnrichProspectDto request, CancellationToken ct) =>
        {
            var result = await mediator.Send(new EnrichProspectCommand(id, request), ct);
            return result.Match(
                () => Results.NoContent(),
                errors => Results.BadRequest(errors));
        })
        .WithName("EnrichProspect")
        .WithTags("Prospects")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
