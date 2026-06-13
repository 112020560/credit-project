using MediatR;
using Credit.Application.Commands;
using SharedKernel;
using Credit.WebApi.Extensions;
using Credit.WebApi.Infrastructure;
using Asp.Versioning;
using Credit.Application.Queries;
using Credit.Application.UseCases.Applications.Dtos;

namespace Credit.WebApi.Endpoints.Credit;

public class Applications: IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/credit/applications", async (IMediator mediator, CreateCreditApplicationCommand command, CancellationToken cancellationToken) =>
        {
            Result result = await mediator.Send(command, cancellationToken);
            return result.Match(
                onSuccess: () => Results.Ok(),
                onFailure: error => CustomResults.Problem(error)
                );
        })
        .WithName("CreateCreditApplication")
        .WithTags(Tags.CreditApplication)
        .MapToApiVersion(new ApiVersion(1));

        app.MapGet("/credit/applications/{id}", async (IMediator mediator, Guid id, CancellationToken cancellationToken) =>
        {
            Result result = await mediator.Send(new GetCreditApplicationByIdQuery(id), cancellationToken);
            return result.Match(
                onSuccess: () => Results.Ok(),
                onFailure: error => CustomResults.Problem(error)
                );
        })
        .WithName("GetCreditApplicationById")
        .WithTags(Tags.CreditApplication)
        .MapToApiVersion(new ApiVersion(1));

        app.MapPost("/credit/applications/{id}/approve", async (IMediator mediator, Guid id, ApproveApplicationDto? body, CancellationToken cancellationToken) =>
        {
            // Placeholder for ApproveCreditApplicationCommand
            Result result = await mediator.Send(new ApproveCreditApplicationCommand(id), cancellationToken);
            return result.Match(
                onSuccess: () => Results.Ok(),
                onFailure: error => CustomResults.Problem(error)
                );
        })
        .WithName("ApproveCreditApplication")
        .WithTags(Tags.CreditApplication)
        .MapToApiVersion(new ApiVersion(1));

        app.MapPost("/credit/applications/{id}/reject", async (IMediator mediator, Guid id, RejectCreditApplication body, CancellationToken cancellationToken) =>
        {
            Result result = await mediator.Send(new RejectCreditApplicationCommand(id, body), cancellationToken);
            return result.Match(
                onSuccess: () => Results.Ok(),
                onFailure: error => CustomResults.Problem(error)
                );
        })
        .WithName("RejectCreditApplication")
        .WithTags(Tags.CreditApplication)
        .MapToApiVersion(new ApiVersion(1));
    }
}