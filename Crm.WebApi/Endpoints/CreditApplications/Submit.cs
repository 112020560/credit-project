using Asp.Versioning;
using Crm.Application.CreditApplications;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.CreditApplications;

public class CreditApplicationsSubmit : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/credit-applications/{id:guid}/submit", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SubmitCreditApplicationCommand(id), ct);
            return result.Match(
                () => Results.NoContent(),
                errors => Results.BadRequest(errors));
        })
        .WithName("SubmitCreditApplication")
        .WithTags("CreditApplications")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
