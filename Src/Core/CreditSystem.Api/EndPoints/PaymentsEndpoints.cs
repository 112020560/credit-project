using CreditSystem.Api.EndPoints.Dtos;
using CreditSystem.Application.Commands.SubmitPayment;
using CreditSystem.Application.Commands.SubmitRevolvingPayment;
using CreditSystem.Application.Commands.SubscribeWebhook;
using CreditSystem.Application.Queries.GetPaymentStatus;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CreditSystem.Api.EndPoints;

public static class PaymentsEndpoints
{
    public static void MapPaymentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments")
            .WithTags("Async Payments")
            .WithOpenApi();

        // POST /api/payments - Submit loan payment (async)
        group.MapPost("/", SubmitPayment)
            .WithName("SubmitPayment")
            .WithSummary("Submit a loan payment for asynchronous processing")
            .WithDescription("Returns immediately with a tracking ID. Use GET /api/payments/{id}/status to check progress.")
            .Produces<PaymentAcceptedResponse>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // POST /api/payments/revolving - Submit revolving credit payment (async)
        group.MapPost("/revolving", SubmitRevolvingPayment)
            .WithName("SubmitRevolvingPayment")
            .WithSummary("Submit a revolving credit payment for asynchronous processing")
            .WithDescription("Returns immediately with a tracking ID. Use GET /api/payments/{id}/status to check progress.")
            .Produces<RevolvingPaymentAcceptedResponse>(StatusCodes.Status202Accepted)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // GET /api/payments/{id}/status - Get payment status
        group.MapGet("/{paymentId:guid}/status", GetPaymentStatus)
            .WithName("GetPaymentStatus")
            .WithSummary("Get the current status of an asynchronous payment")
            .Produces<PaymentStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    public static void MapWebhooksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/webhooks")
            .WithTags("Webhooks")
            .WithOpenApi();

        // POST /api/webhooks/subscribe - Subscribe to webhook
        group.MapPost("/subscribe", SubscribeWebhook)
            .WithName("SubscribeWebhook")
            .WithSummary("Subscribe to payment event notifications")
            .WithDescription("Register a callback URL to receive notifications when payments are processed.")
            .Produces<SubscribeWebhookResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> SubmitPayment(
        [FromBody] SubmitPaymentRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new SubmitPaymentCommand(
                LoanId: request.LoanId,
                CustomerId: request.CustomerId,
                Amount: request.Amount,
                Currency: request.Currency ?? "MXN",
                PaymentMethod: request.PaymentMethod
            );

            var response = await mediator.Send(command, cancellationToken);

            if (!response.IsAccepted)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Payment Rejected",
                    Detail = response.ErrorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            return Results.Accepted(response.TrackingUrl, response);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred",
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errors"] = ex.Errors.Select(e => e.ErrorMessage).ToList() }
            });
        }
    }

    private static async Task<IResult> SubmitRevolvingPayment(
        [FromBody] SubmitRevolvingPaymentRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new SubmitRevolvingPaymentCommand(
                CreditLineId: request.CreditLineId,
                CustomerId: request.CustomerId,
                Amount: request.Amount,
                Currency: request.Currency ?? "MXN",
                PaymentMethod: request.PaymentMethod
            );

            var response = await mediator.Send(command, cancellationToken);

            if (!response.IsAccepted)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Payment Rejected",
                    Detail = response.ErrorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            return Results.Accepted(response.TrackingUrl, response);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred",
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errors"] = ex.Errors.Select(e => e.ErrorMessage).ToList() }
            });
        }
    }

    private static async Task<IResult> GetPaymentStatus(
        Guid paymentId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetPaymentStatusQuery(paymentId);
        var response = await mediator.Send(query, cancellationToken);

        return response == null
            ? Results.NotFound()
            : Results.Ok(response);
    }

    private static async Task<IResult> SubscribeWebhook(
        [FromBody] SubscribeWebhookRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new SubscribeWebhookCommand(
                CustomerId: request.CustomerId,
                EventType: request.EventType,
                CallbackUrl: request.CallbackUrl,
                SecretKey: request.SecretKey
            );

            var response = await mediator.Send(command, cancellationToken);

            if (!response.IsSuccess)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Subscription Failed",
                    Detail = response.ErrorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            return Results.Created($"/api/webhooks/{response.SubscriptionId}", response);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred",
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errors"] = ex.Errors.Select(e => e.ErrorMessage).ToList() }
            });
        }
    }
}
