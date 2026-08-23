using Asp.Versioning;
using Crm.Application.Documents;
using Crm.Application.Documents.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.Documents;

public class DocumentsValidate : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/documents/{id:guid}/validate", async (Guid id, ValidateDocumentDto dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ValidateDocumentCommand(id, dto), ct);
            return result.Match(
                () => Results.NoContent(),
                errors => Results.BadRequest(errors));
        })
        .WithName("ValidateDocument")
        .WithTags("Documents")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
