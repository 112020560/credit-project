using MediatR;
using Credit.Application.Commands;
using Credit.Application.Queries;
using Credit.WebApi.Extensions;
using Credit.WebApi.Infrastructure;
using Asp.Versioning;
using Credit.Application.UseCases.Payments;
using Credit.Application.UseCases.Payments.Dtos;
using Credit.Application.UseCases.CreditLine;

namespace Credit.WebApi.Endpoints.Credit;

public class Lines: IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/credit/lines", async (IMediator mediator, OpenCreditLineCommand command, CancellationToken token) =>
        {
            var result = await mediator.Send(command, token);
            return result.Match(
                onSuccess: lineId => Results.Ok(lineId),
                onFailure: error => CustomResults.Problem(error)
                );
        })
        .WithName("OpenCreditLine")
        .WithTags(Tags.CreditLines)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1));

        app.MapGet("/credit/lines/{id}", async (IMediator mediator, Guid id, CancellationToken token) =>
        {
            var result = await mediator.Send(new GetCreditLineByIdQuery(id), token);
            return result.Match(
                onSuccess: line => Results.Ok(line),
                onFailure: error => CustomResults.Problem(error)
                );
        })
        .WithName("GetCreditLineById")
        .WithTags(Tags.CreditLines)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1));

        app.MapGet("/credit/lines/{id}/schedule", async (Guid id, IMediator mediator, CancellationToken token) =>
        {
            var result = await mediator.Send(new GetLinePaymentScheduleByIdQuery(id), token);
            return result.Match(
                onSuccess: lines => Results.Ok(lines),
                onFailure: error => CustomResults.Problem(error)
                );
        })
        .WithName("GetLinePaymentScheduleById")
        .WithTags(Tags.Payments)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1));

        app.MapGet("/credit/lines/{id}/installments", async (Guid id, IMediator mediator, CancellationToken token) =>
        {
            var result = await mediator.Send(new GetLineInstallmentsByIdQuery(id), token);
            return result.Match(
                onSuccess: lines => Results.Ok(lines),
                onFailure: error => CustomResults.Problem(error)
                );
        })
        .WithName("GetLineInstallmentsById")
        .WithTags(Tags.Payments)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1));


        app.MapPost("/credit/lines/{id}/payments", async (Guid id, CreatePaymentsDto body, IMediator mediator, CancellationToken token) =>
        {
            var result = await mediator.Send(new RegisterPaymentCommand(id, body), token);
            return result.Match(
                onSuccess: lines => Results.Ok(lines),
                onFailure: error => CustomResults.Problem(error)
                );
        })
        .WithName("GetLinePaymentsById")
        .WithTags(Tags.Payments)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1));

        app.MapGet("/credit/lines/{id}/balance", async (Guid id, IMediator mediator, CancellationToken token) =>
        {
            var result = await mediator.Send(new GetCreditLineBalanceByIdQuery(id), token);
            return result.Match(
                onSuccess: balance => Results.Ok(balance),
                onFailure: error => CustomResults.Problem(error)
                );
        })
        .WithName("GetCreditLineBalanceById")
        .WithTags(Tags.CreditLines)
        .WithOpenApi()
        .MapToApiVersion(new ApiVersion(1));
    }
}