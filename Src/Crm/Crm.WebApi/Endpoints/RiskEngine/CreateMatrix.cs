using Asp.Versioning;
using Crm.Application.RiskEngine;
using Crm.Application.RiskEngine.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.RiskEngine;

public class RiskEngineCreateMatrix : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/risk-matrices", async (CreateRiskMatrixDto dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateRiskMatrixCommand(dto), ct);
            return result.Match(
                matrix => Results.Created($"/api/v1/risk-matrices/{matrix.Id}", matrix),
                errors => Results.BadRequest(errors));
        })
        .WithName("CreateRiskMatrix")
        .WithTags("RiskEngine")
        .Produces<RiskMatrixDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
