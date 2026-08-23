using Asp.Versioning;
using Crm.Application.RiskEngine;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.RiskEngine;

public class RiskEngineActivateMatrix : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/risk-matrices/{id:guid}/activate", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ActivateRiskMatrixCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                errors => Results.BadRequest(errors));
        })
        .WithName("ActivateRiskMatrix")
        .WithTags("RiskEngine")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
