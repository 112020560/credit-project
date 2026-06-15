using Asp.Versioning;
using Crm.Application.ApprovalWorkflows;
using Crm.Application.ApprovalWorkflows.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.ApprovalWorkflows;

public class ApprovalWorkflowsCreate : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/workflows", async (CreateWorkflowDefinitionDto dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateWorkflowDefinitionCommand(dto), ct);
            return result.Match(
                wf => Results.Created($"/api/v1/workflows/{wf.Id}", wf),
                errors => Results.BadRequest(errors));
        })
        .WithName("CreateWorkflow")
        .WithTags("ApprovalWorkflows")
        .Produces<WorkflowDefinitionDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
