using Asp.Versioning;
using Crm.Application.CreditApplications;
using Crm.Application.CreditApplications.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.CreditApplications;

public class CreditApplicationsReject : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/credit-applications/{id:guid}/reject", async (Guid id, IMediator mediator, RejectCreditApplicationDto request, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RejectCreditApplicationCommand(id, request), ct);
            return result.Match(
                () => Results.NoContent(),
                errors => Results.BadRequest(errors));
        })
        .WithName("RejectCreditApplication")
        .WithTags("CreditApplications")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
