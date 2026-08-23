using Asp.Versioning;
using Crm.Application.ApprovalWorkflows;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.ApprovalWorkflows;

public class ApprovalWorkflowsActivate : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/workflows/{id:guid}/activate", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ActivateWorkflowDefinitionCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                errors => Results.BadRequest(errors));
        })
        .WithName("ActivateWorkflow")
        .WithTags("ApprovalWorkflows")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
