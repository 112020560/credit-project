using Asp.Versioning;
using Crm.Application.RiskEngine;
using Crm.Application.RiskEngine.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.RiskEngine;

public class RiskEngineCreateRule : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/risk-rules", async (CreateRiskRuleDto dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateRiskRuleCommand(dto), ct);
            return result.Match(
                rule => Results.Created($"/api/v1/risk-rules/{rule.Id}", rule),
                errors => Results.BadRequest(errors));
        })
        .WithName("CreateRiskRule")
        .WithTags("RiskEngine")
        .Produces<RiskRuleDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
