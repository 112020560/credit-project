using Asp.Versioning;
using Crm.Application.Prospects;
using Crm.Application.Prospects.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.Prospects;

public class ProspectsCreate : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/prospects", async (IMediator mediator, CreateProspectDto request, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateProspectCommand(request), ct);
            return result.Match(
                dto => Results.Created($"/prospects/{dto.Id}", dto),
                errors => Results.BadRequest(errors));
        })
        .WithName("CreateProspect")
        .WithTags("Prospects")
        .Produces<ProspectSummaryDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
