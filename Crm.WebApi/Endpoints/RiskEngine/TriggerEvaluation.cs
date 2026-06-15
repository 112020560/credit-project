using Asp.Versioning;
using Crm.Application.RiskEngine;
using Crm.Application.RiskEngine.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.RiskEngine;

public class RiskEngineTriggerEvaluation : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/risk-evaluations", async (TriggerRiskEvaluationDto dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new TriggerRiskEvaluationCommand(dto), ct);
            return result.Match(
                evaluation => Results.Created($"/api/v1/risk-evaluations/{evaluation.Id}", evaluation),
                errors => Results.BadRequest(errors));
        })
        .WithName("TriggerRiskEvaluation")
        .WithTags("RiskEngine")
        .Produces<RiskEvaluationDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
