using Asp.Versioning;
using Crm.Application.Documents;
using Crm.Application.Documents.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.Documents;

public class DocumentsGetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/documents/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetDocumentByIdQuery(id), ct);
            return result.Match(
                doc => Results.Ok(doc),
                errors => Results.NotFound(errors));
        })
        .WithName("GetDocumentById")
        .WithTags("Documents")
        .Produces<DocumentDetailDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
