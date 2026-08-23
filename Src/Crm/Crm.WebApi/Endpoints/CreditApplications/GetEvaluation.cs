using Asp.Versioning;
using Crm.Application.RiskEngine;
using Crm.Application.RiskEngine.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.CreditApplications;

public class CreditApplicationsGetEvaluation : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/applications/{id:guid}/evaluation", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetRiskEvaluationByApplicationIdQuery(id), ct);
            return result.Match(
                evaluation => Results.Ok(evaluation),
                errors => Results.NotFound(errors));
        })
        .WithName("GetCreditApplicationEvaluation")
        .WithTags("CreditApplications")
        .Produces<RiskEvaluationDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
