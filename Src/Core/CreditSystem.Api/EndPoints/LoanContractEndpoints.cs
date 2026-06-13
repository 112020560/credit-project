using CreditSystem.Api.EndPoints.Dtos;
using CreditSystem.Application.Commands.ApplyPayment;
using CreditSystem.Application.Commands.CreateContract;
using CreditSystem.Application.Commands.DefaultContract;
using CreditSystem.Application.Commands.DisburseLoan;
using CreditSystem.Application.Commands.PayoffContract;
using CreditSystem.Application.Commands.RestructureContract;
using CreditSystem.Application.Queries;
using CreditSystem.Application.Queries.GetDefaultedLoans;
using CreditSystem.Application.Queries.GetDelinquentLoans;
using CreditSystem.Application.Queries.GetLoanSummary;
using CreditSystem.Application.Queries.GetPaidOffLoans;
using CreditSystem.Application.Queries.GetPaymentHistory;
using CreditSystem.Application.Queries.GetPayoffAmount;
using CreditSystem.Application.Queries.GetRestructureHistory;
using CreditSystem.Domain.Abstractions.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CreditSystem.Api.EndPoints;

public static class LoanContractEndpoints
{
    public static void MapLoanContractEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/loans")
            .WithTags("Loan Contracts")
            .WithOpenApi();

        // POST /api/loans - Crear contrato
        group.MapPost("/", CreateContract)
            .WithName("CreateContract")
            .WithSummary("Create a new loan contract")
            .Produces<CreateContractResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);


        // POST /api/loans/{id}/disburse - Desembolsar
        group.MapPost("/{id:guid}/disburse", DisburseLoan)
            .WithName("DisburseLoan")
            .WithSummary("Disburse an approved loan")
            .Produces<DisburseLoanResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // GET /api/loans/{id} - Obtener resumen
        group.MapGet("/{id:guid}", GetLoanSummary)
            .WithName("GetLoanSummary")
            .WithSummary("Get loan summary by ID")
            .Produces<LoanSummaryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/loans/customer/{externalCustomerId} - Préstamos por cliente
        group.MapGet("/customer/{externalCustomerId:guid}", GetCustomerLoans)
            .WithName("GetCustomerLoans")
            .WithSummary("Get all loans for a customer")
            .Produces<IReadOnlyList<LoanSummaryResponse>>(StatusCodes.Status200OK);


        // POST /api/loans/{id}/payments - Aplicar pago
        group.MapPost("/{id:guid}/payments", ApplyPayment)
            .WithName("ApplyPayment")
            .WithSummary("Apply a payment to a loan")
            .Produces<ApplyPaymentResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // GET /api/loans/{id}/payments - Historial de pagos
        group.MapGet("/{id:guid}/payments", GetPaymentHistory)
            .WithName("GetPaymentHistory")
            .WithSummary("Get payment history for a loan")
            .Produces<IReadOnlyList<PaymentHistoryResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/loans/{id}/default - Marcar como default
        group.MapPost("/{id:guid}/default", DefaultContract)
            .WithName("DefaultContract")
            .WithSummary("Mark a loan as defaulted")
            .Produces<DefaultContractResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // GET /api/loans/defaulted - Listar préstamos en default
        group.MapGet("/defaulted", GetDefaultedLoans)
            .WithName("GetDefaultedLoans")
            .WithSummary("Get all defaulted loans")
            .Produces<IReadOnlyList<DefaultedLoanResponse>>(StatusCodes.Status200OK);


        // POST /api/loans/{id}/restructure - Reestructurar préstamo
        group.MapPost("/{id:guid}/restructure", RestructureContract)
            .WithName("RestructureContract")
            .WithSummary("Restructure a delinquent or defaulted loan")
            .Produces<RestructureContractResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // GET /api/loans/{id}/restructure-history - Historial de reestructuraciones
        group.MapGet("/{id:guid}/restructure-history", GetRestructureHistory)
            .WithName("GetRestructureHistory")
            .WithSummary("Get restructure history for a loan")
            .Produces<IReadOnlyList<RestructureHistoryResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/loans/{id}/payoff-amount - Obtener monto de payoff
        group.MapGet("/{id:guid}/payoff-amount", GetPayoffAmount)
            .WithName("GetPayoffAmount")
            .WithSummary("Get the payoff amount for a loan")
            .Produces<PayoffAmountResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/loans/{id}/payoff - Pagar préstamo completo
        group.MapPost("/{id:guid}/payoff", PayoffContract)
            .WithName("PayoffContract")
            .WithSummary("Pay off a loan completely")
            .Produces<PayoffContractResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // GET /api/loans/paid-off - Listar préstamos pagados
        group.MapGet("/paid-off", GetPaidOffLoans)
            .WithName("GetPaidOffLoans")
            .WithSummary("Get all paid off loans")
            .Produces<IReadOnlyList<PaidOffLoanResponse>>(StatusCodes.Status200OK);
    }

    public static void MapDelinquentLoansEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/delinquent-loans")
            .WithTags("Delinquent Loans")
            .WithOpenApi();

        // GET /api/delinquent-loans - Listar préstamos morosos
        group.MapGet("/", GetDelinquentLoans)
            .WithName("GetDelinquentLoans")
            .WithSummary("Get all delinquent loans")
            .Produces<IReadOnlyList<DelinquentLoanResponse>>(StatusCodes.Status200OK);

        // GET /api/delinquent-loans/{id} - Detalle de préstamo moroso
        group.MapGet("/{id:guid}", GetDelinquentLoanDetail)
            .WithName("GetDelinquentLoanDetail")
            .WithSummary("Get delinquent loan details")
            .Produces<DelinquentLoanResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
    private static async Task<IResult> CreateContract(
        [FromBody] CreateContractCommand command,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await mediator.Send(command, cancellationToken);

            if (!response.Success)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Contract Creation Failed",
                    Detail = response.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Extensions = { ["errors"] = response.Errors }
                });
            }

            return Results.Created($"/api/loans/{response.ContractId}", response);
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

    private static async Task<IResult> DisburseLoan(
        Guid id,
        [FromBody] DisburseLoanRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new DisburseLoanCommand
            {
                LoanId = id,
                DisbursementMethod = request.DisbursementMethod,
                DestinationAccount = request.DestinationAccount
            };

            var response = await mediator.Send(command, cancellationToken);

            if (!response.Success)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Disbursement Failed",
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

    private static async Task<IResult> GetLoanSummary(
        Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetLoanSummaryQuery { LoanId = id };
        var response = await mediator.Send(query, cancellationToken);

        return response == null
            ? Results.NotFound()
            : Results.Ok(response);
    }

    private static async Task<IResult> GetCustomerLoans(
        Guid externalCustomerId,
        [FromServices] IMediator mediator,
        [FromServices] ICustomerService customerService,
        [FromServices] ILoanQueryService queryService,
        CancellationToken cancellationToken)
    {
        var customer = await customerService.GetByExternalIdAsync(externalCustomerId, cancellationToken);

        if (customer == null)
            return Results.NotFound();

        var loans = await queryService.GetCustomerLoansAsync(customer.Id, cancellationToken);
        var response = loans.Select(LoanSummaryResponse.FromReadModel).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> ApplyPayment(
        Guid id,
        [FromBody] ApplyPaymentRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new ApplyPaymentCommand
            {
                LoanId = id,
                Amount = request.Amount,
                Currency = request.Currency ?? "USD",
                PaymentMethod = request.PaymentMethod,
                ReferenceNumber = request.ReferenceNumber
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

    private static async Task<IResult> GetPaymentHistory(
        Guid id,
        [FromServices] IMediator mediator,
        [FromServices] ILoanQueryService queryService,
        CancellationToken cancellationToken)
    {
        // Verificar que el préstamo existe
        var loan = await queryService.GetLoanSummaryAsync(id, cancellationToken);

        if (loan == null)
            return Results.NotFound();

        var query = new GetPaymentHistoryQuery { LoanId = id };
        var response = await mediator.Send(query, cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetDelinquentLoans(
        [FromQuery] int? minDaysOverdue,
        [FromQuery] string? collectionStatus,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetDelinquentLoansQuery
        {
            MinDaysOverdue = minDaysOverdue,
            CollectionStatus = collectionStatus
        };

        var response = await mediator.Send(query, cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetDelinquentLoanDetail(
        Guid id,
        [FromServices] ILoanQueryService queryService,
        CancellationToken cancellationToken)
    {
        var loans = await queryService.GetDelinquentLoansAsync(ct: cancellationToken);
        var loan = loans.FirstOrDefault(l => l.LoanId == id);

        return loan == null
            ? Results.NotFound()
            : Results.Ok(DelinquentLoanResponse.FromReadModel(loan));
    }

    private static async Task<IResult> DefaultContract(
        Guid id,
        [FromBody] DefaultContractRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new DefaultContractCommand
            {
                LoanId = id,
                Reason = request.Reason
            };

            var response = await mediator.Send(command, cancellationToken);

            if (!response.Success)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Default Failed",
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

    private static async Task<IResult> GetDefaultedLoans(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetDefaultedLoansQuery
        {
            FromDate = fromDate,
            ToDate = toDate
        };

        var response = await mediator.Send(query, cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> RestructureContract(
        Guid id,
        [FromBody] RestructureContractRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new RestructureContractCommand
            {
                LoanId = id,
                NewInterestRate = request.NewInterestRate,
                NewTermMonths = request.NewTermMonths,
                ForgiveAmount = request.ForgiveAmount,
                Reason = request.Reason
            };

            var response = await mediator.Send(command, cancellationToken);

            if (!response.Success)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Restructure Failed",
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

    private static async Task<IResult> GetRestructureHistory(
        Guid id,
        [FromServices] IMediator mediator,
        [FromServices] ILoanQueryService queryService,
        CancellationToken cancellationToken)
    {
        // Verificar que el préstamo existe
        var loan = await queryService.GetLoanSummaryAsync(id, cancellationToken);

        if (loan == null)
            return Results.NotFound();

        var query = new GetRestructureHistoryQuery { LoanId = id };
        var response = await mediator.Send(query, cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetPayoffAmount(
    Guid id,
    [FromQuery] DateTime? asOfDate,
    [FromServices] IMediator mediator,
    CancellationToken cancellationToken)
    {
        var query = new GetPayoffAmountQuery
        {
            LoanId = id,
            AsOfDate = asOfDate
        };

        var response = await mediator.Send(query, cancellationToken);

        return response == null
            ? Results.NotFound()
            : Results.Ok(response);
    }

    private static async Task<IResult> PayoffContract(
        Guid id,
        [FromBody] PayoffContractRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new PayoffContractCommand
            {
                LoanId = id,
                PaymentMethod = request.PaymentMethod,
                ReferenceNumber = request.ReferenceNumber
            };

            var response = await mediator.Send(command, cancellationToken);

            if (!response.Success)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Payoff Failed",
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

    private static async Task<IResult> GetPaidOffLoans(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] bool? earlyPayoffOnly,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetPaidOffLoansQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            EarlyPayoffOnly = earlyPayoffOnly
        };

        var response = await mediator.Send(query, cancellationToken);
        return Results.Ok(response);
    }
}