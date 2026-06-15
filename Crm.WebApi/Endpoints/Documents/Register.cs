using Asp.Versioning;
using Crm.Application.Documents;
using Crm.Application.Documents.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.Documents;

public class DocumentsRegister : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/documents", async (RegisterDocumentDto dto, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RegisterDocumentCommand(dto), ct);
            return result.Match(
                doc => Results.Created($"/api/v1/documents/{doc.Id}", doc),
                errors => Results.BadRequest(errors));
        })
        .WithName("RegisterDocument")
        .WithTags("Documents")
        .Produces<DocumentDetailDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
