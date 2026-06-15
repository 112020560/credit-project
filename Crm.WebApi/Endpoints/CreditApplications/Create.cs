using Asp.Versioning;
using Crm.Application.CreditApplications;
using Crm.Application.CreditApplications.Dtos;
using Crm.WebApi.Extensions;
using MediatR;

namespace Crm.WebApi.Endpoints.CreditApplications;

public class CreditApplicationsCreate : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/credit-applications", async (IMediator mediator, CreateCreditApplicationDto request, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateCreditApplicationCommand(request), ct);
            return result.Match(
                dto => Results.Created($"/credit-applications/{dto.Id}", dto),
                errors => Results.BadRequest(errors));
        })
        .WithName("CreateCreditApplication")
        .WithTags("CreditApplications")
        .Produces<CreditApplicationDetailDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1, 0));
    }
}
