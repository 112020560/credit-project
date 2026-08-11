using CreditSystem.Application.Queries.Documents;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using DocFormat = CreditSystem.Domain.Models.Documents.DocumentFormat;

namespace CreditSystem.Api.EndPoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/loans/{id}/documents")
            .WithTags("Documents")
            .WithOpenApi();

        group.MapGet("/payment-receipt/{paymentId}", GetPaymentReceipt)
            .WithName("GetPaymentReceipt")
            .WithSummary("Download payment receipt PDF")
            .Produces<byte[]>(StatusCodes.Status200OK, "application/pdf")
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/balance-letter", GetBalanceLetter)
            .WithName("GetBalanceLetter")
            .WithSummary("Download balance letter PDF")
            .Produces<byte[]>(StatusCodes.Status200OK, "application/pdf")
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/amortization-table", GetAmortizationTable)
            .WithName("GetAmortizationTable")
            .WithSummary("Download amortization table as PDF or Excel")
            .Produces<byte[]>(StatusCodes.Status200OK, "application/pdf")
            .Produces<byte[]>(StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetPaymentReceipt(
        Guid id,
        Guid paymentId,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var bytes = await mediator.Send(new GetPaymentReceiptQuery(id, paymentId), ct);
        if (bytes is null) return Results.NotFound();

        return Results.File(bytes, "application/pdf", $"comprobante-{paymentId:N}.pdf");
    }

    private static async Task<IResult> GetBalanceLetter(
        Guid id,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var bytes = await mediator.Send(new GetBalanceLetterQuery(id), ct);
        if (bytes is null) return Results.NotFound();

        return Results.File(bytes, "application/pdf", $"carta-saldo-{id:N}.pdf");
    }

    private static async Task<IResult> GetAmortizationTable(
        Guid id,
        [FromQuery] string format,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        DocFormat documentFormat;
        string contentType;
        string fileName;

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            documentFormat = DocFormat.Pdf;
            contentType = "application/pdf";
            fileName = $"tabla-amortizacion-{id:N}.pdf";
        }
        else if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            documentFormat = DocFormat.Excel;
            contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            fileName = $"tabla-amortizacion-{id:N}.xlsx";
        }
        else
        {
            return Results.Problem(
                detail: "Formato no válido. Use 'pdf' o 'xlsx'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var bytes = await mediator.Send(new GetAmortizationTableQuery(id, documentFormat), ct);
        if (bytes is null) return Results.NotFound();

        return Results.File(bytes, contentType, fileName);
    }
}
