// Api/Endpoints/RevolvingCreditEndpoints.cs

using CreditSystem.Api.EndPoints.Dtos;
using CreditSystem.Application.Commands.RevolvingCredit.ActivateCreditLine;
using CreditSystem.Application.Commands.RevolvingCredit.ApplyRevolvingPayment;
using CreditSystem.Application.Commands.RevolvingCredit.ChangeCreditLimit;
using CreditSystem.Application.Commands.RevolvingCredit.CloseCreditLine;
using CreditSystem.Application.Commands.RevolvingCredit.CreateCreditLine;
using CreditSystem.Application.Commands.RevolvingCredit.DrawFunds;
using CreditSystem.Application.Commands.RevolvingCredit.FreezeCreditLine;
using CreditSystem.Application.Commands.RevolvingCredit.UnfreezeCreditLine;
using CreditSystem.Application.Queries.RevolvingCredit;
using CreditSystem.Application.Queries.RevolvingCredit.GetRevolvingCreditSummary;
using CreditSystem.Application.Queries.RevolvingCredit.GetRevolvingTransactions;
using CreditSystem.Domain.Abstractions.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CreditSystem.Api.Endpoints;

public static class RevolvingCreditEndpoints
{
    public static void MapRevolvingCreditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/revolving-credits")
            .WithTags("Revolving Credit")
            .WithOpenApi();

        // POST /api/revolving-credits - Crear línea de crédito
        group.MapPost("/", CreateCreditLine)
            .WithName("CreateCreditLine")
            .WithSummary("Create a new revolving credit line")
            .Produces<CreateCreditLineResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // POST /api/revolving-credits/{id}/activate - Activar línea
        group.MapPost("/{id:guid}/activate", ActivateCreditLine)
            .WithName("ActivateCreditLine")
            .WithSummary("Activate a pending credit line")
            .Produces<ActivateCreditLineResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // POST /api/revolving-credits/{id}/draw - Disponer fondos
        group.MapPost("/{id:guid}/draw", DrawFunds)
            .WithName("DrawFunds")
            .WithSummary("Draw funds from credit line")
            .Produces<DrawFundsResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // POST /api/revolving-credits/{id}/payments - Aplicar pago
        group.MapPost("/{id:guid}/payments", ApplyPayment)
            .WithName("ApplyRevolvingPayment")
            .WithSummary("Apply payment to credit line")
            .Produces<ApplyRevolvingPaymentResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // POST /api/revolving-credits/{id}/change-limit - Cambiar límite
        group.MapPost("/{id:guid}/change-limit", ChangeCreditLimit)
            .WithName("ChangeCreditLimit")
            .WithSummary("Change credit limit")
            .Produces<ChangeCreditLimitResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // POST /api/revolving-credits/{id}/freeze - Congelar
        group.MapPost("/{id:guid}/freeze", FreezeCreditLine)
            .WithName("FreezeCreditLine")
            .WithSummary("Freeze credit line")
            .Produces<FreezeCreditLineResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // POST /api/revolving-credits/{id}/unfreeze - Descongelar
        group.MapPost("/{id:guid}/unfreeze", UnfreezeCreditLine)
            .WithName("UnfreezeCreditLine")
            .WithSummary("Unfreeze credit line")
            .Produces<UnfreezeCreditLineResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // POST /api/revolving-credits/{id}/close - Cerrar
        group.MapPost("/{id:guid}/close", CloseCreditLine)
            .WithName("CloseCreditLine")
            .WithSummary("Close credit line")
            .Produces<CloseCreditLineResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // GET /api/revolving-credits/{id} - Obtener resumen
        group.MapGet("/{id:guid}", GetCreditLineSummary)
            .WithName("GetCreditLineSummary")
            .WithSummary("Get credit line summary")
            .Produces<RevolvingCreditSummaryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/revolving-credits/{id}/transactions - Obtener transacciones
        group.MapGet("/{id:guid}/transactions", GetTransactions)
            .WithName("GetRevolvingTransactions")
            .WithSummary("Get credit line transactions")
            .Produces<IReadOnlyList<RevolvingTransactionResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/revolving-credits/{id}/statements - Obtener estados de cuenta
        group.MapGet("/{id:guid}/statements", GetStatements)
            .WithName("GetRevolvingStatements")
            .WithSummary("Get credit line statements")
            .Produces<IReadOnlyList<RevolvingStatementResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/revolving-credits/customer/{customerId} - Obtener por cliente
        group.MapGet("/customer/{customerId:guid}", GetByCustomer)
            .WithName("GetCreditLinesByCustomer")
            .WithSummary("Get all credit lines for a customer")
            .Produces<IReadOnlyList<RevolvingCreditSummaryResponse>>(StatusCodes.Status200OK);
    }

    #region Command Handlers

    private static async Task<IResult> CreateCreditLine(
        [FromBody] CreateCreditLineRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateCreditLineCommand
            {
                ExternalCustomerId = request.ExternalCustomerId,
                CreditLimit = request.CreditLimit,
                Currency = request.Currency ?? "USD",
                InterestRate = request.InterestRate,
                MinimumPaymentPercentage = request.MinimumPaymentPercentage ?? 5,
                MinimumPaymentAmount = request.MinimumPaymentAmount ?? 25,
                BillingCycleDay = request.BillingCycleDay ?? 15,
                GracePeriodDays = request.GracePeriodDays ?? 20
            };

            var response = await mediator.Send(command, cancellationToken);

            if (!response.Success)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Credit Line Creation Failed",
                    Detail = response.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["errors"] = response.Errors }
                });
            }

            return Results.Created($"/api/revolving-credits/{response.CreditLineId}", response);
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

    private static async Task<IResult> ActivateCreditLine(
        Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new ActivateCreditLineCommand { CreditLineId = id };
        var response = await mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Activation Failed",
                Detail = response.Message,
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errors"] = response.Errors }
            });
        }

        return Results.Ok(response);
    }

    private static async Task<IResult> DrawFunds(
        Guid id,
        [FromBody] DrawFundsRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new DrawFundsCommand
            {
                CreditLineId = id,
                Amount = request.Amount,
                Currency = request.Currency ?? "USD",
                Description = request.Description
            };

            var response = await mediator.Send(command, cancellationToken);

            if (!response.Success)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Draw Failed",
                    Detail = response.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["errors"] = response.Errors }
                });
            }

            return Results.Ok(response);
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

    private static async Task<IResult> ApplyPayment(
        Guid id,
        [FromBody] ApplyRevolvingPaymentRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new ApplyRevolvingPaymentCommand
            {
                CreditLineId = id,
                Amount = request.Amount,
                Currency = request.Currency ?? "USD",
                PaymentMethod = request.PaymentMethod
            };

            var response = await mediator.Send(command, cancellationToken);

            if (!response.Success)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Payment Failed",
                    Detail = response.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["errors"] = response.Errors }
                });
            }

            return Results.Ok(response);
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

    private static async Task<IResult> ChangeCreditLimit(
        Guid id,
        [FromBody] ChangeCreditLimitRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new ChangeCreditLimitCommand
            {
                CreditLineId = id,
                NewLimit = request.NewLimit,
                Currency = request.Currency ?? "USD",
                Reason = request.Reason
            };

            var response = await mediator.Send(command, cancellationToken);

            if (!response.Success)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Limit Change Failed",
                    Detail = response.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["errors"] = response.Errors }
                });
            }

            return Results.Ok(response);
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

    private static async Task<IResult> FreezeCreditLine(
        Guid id,
        [FromBody] FreezeCreditLineRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new FreezeCreditLineCommand
            {
                CreditLineId = id,
                Reason = request.Reason
            };

            var response = await mediator.Send(command, cancellationToken);

            if (!response.Success)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Freeze Failed",
                    Detail = response.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["errors"] = response.Errors }
                });
            }

            return Results.Ok(response);
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

    private static async Task<IResult> UnfreezeCreditLine(
        Guid id,
        [FromBody] UnfreezeCreditLineRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UnfreezeCreditLineCommand
            {
                CreditLineId = id,
                Reason = request.Reason
            };

            var response = await mediator.Send(command, cancellationToken);

            if (!response.Success)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Unfreeze Failed",
                    Detail = response.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["errors"] = response.Errors }
                });
            }

            return Results.Ok(response);
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

    private static async Task<IResult> CloseCreditLine(
        Guid id,
        [FromBody] CloseCreditLineRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CloseCreditLineCommand
            {
                CreditLineId = id,
                Reason = request.Reason
            };

            var response = await mediator.Send(command, cancellationToken);

            if (!response.Success)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Close Failed",
                    Detail = response.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["errors"] = response.Errors }
                });
            }

            return Results.Ok(response);
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

    #endregion

    #region Query Handlers

    private static async Task<IResult> GetCreditLineSummary(
        Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetRevolvingCreditSummaryQuery { CreditLineId = id };
        var response = await mediator.Send(query, cancellationToken);

        return response == null
            ? Results.NotFound()
            : Results.Ok(response);
    }

    private static async Task<IResult> GetTransactions(
        Guid id,
        [FromQuery] int? limit,
        [FromServices] IMediator mediator,
        [FromServices] IRevolvingCreditQueryService queryService,
        CancellationToken cancellationToken)
    {
        // Verificar que existe la línea de crédito
        var summary = await queryService.GetSummaryAsync(id, cancellationToken);
        if (summary == null)
            return Results.NotFound();

        var query = new GetRevolvingTransactionsQuery
        {
            CreditLineId = id,
            Limit = limit ?? 50
        };

        var response = await mediator.Send(query, cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetStatements(
        Guid id,
        [FromServices] IRevolvingCreditQueryService queryService,
        CancellationToken cancellationToken)
    {
        // Verificar que existe la línea de crédito
        var summary = await queryService.GetSummaryAsync(id, cancellationToken);
        if (summary == null)
            return Results.NotFound();

        var statements = await queryService.GetStatementsAsync(id, cancellationToken);

        var response = statements.Select(s => new RevolvingStatementResponse
        {
            StatementId = s.StatementId,
            StatementDate = s.StatementDate,
            DueDate = s.DueDate,
            PreviousBalance = s.PreviousBalance,
            Purchases = s.Purchases,
            Payments = s.Payments,
            InterestCharged = s.InterestCharged,
            FeesCharged = s.FeesCharged,
            NewBalance = s.NewBalance,
            MinimumPayment = s.MinimumPayment,
            IsPaid = s.IsPaid,
            PaidAt = s.PaidAt
        }).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetByCustomer(
        Guid customerId,
        [FromServices] ICustomerService customerService,
        [FromServices] IRevolvingCreditQueryService queryService,
        CancellationToken cancellationToken)
    {
        var customer = await customerService.GetByExternalIdAsync(customerId, cancellationToken);
        if (customer == null)
            return Results.NotFound();

        var creditLines = await queryService.GetByCustomerAsync(customer.Id, cancellationToken);

        var response = creditLines
            .Select(RevolvingCreditSummaryResponse.FromReadModel)
            .ToList();

        return Results.Ok(response);
    }

    #endregion
}