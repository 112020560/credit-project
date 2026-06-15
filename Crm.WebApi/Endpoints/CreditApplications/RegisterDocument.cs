using Asp.Versioning;
using Crm.Application.CreditApplications;
using Crm.Application.CreditApplications.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.CreditApplications;

public class CreditApplicationsRegisterDocument : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/credit-applications/{id:guid}/documents", async (Guid id, IMediator mediator, RegisterApplicationDocumentDto request, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RegisterApplicationDocumentCommand(id, request), ct);
            return result.Match(
                dto => Results.Created($"/credit-applications/{id}/documents/{dto.Id}", dto),
                errors => Results.BadRequest(errors));
        })
        .WithName("RegisterApplicationDocument")
        .WithTags("CreditApplications")
        .Produces<ApplicationDocumentDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
