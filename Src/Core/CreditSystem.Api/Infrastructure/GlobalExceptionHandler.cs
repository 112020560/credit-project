using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CreditSystem.Api.Infrastructure;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            BadHttpRequestException { InnerException: JsonException jsonEx } =>
                (StatusCodes.Status400BadRequest, "Invalid request body", jsonEx.Message),

            BadHttpRequestException badReq =>
                (StatusCodes.Status400BadRequest, "Invalid request", badReq.Message),

            _ => (StatusCodes.Status500InternalServerError, "Server failure", null as string)
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception occurred");
        else
            logger.LogWarning(exception, "Bad request: {Title}", title);

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = status == 400
                ? "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1"
                : "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
        };

        // For JSON deserialization errors, extract the field path from the JsonException
        if (exception is BadHttpRequestException { InnerException: JsonException je } && je.Path is not null)
        {
            problemDetails.Extensions["jsonPath"] = je.Path;
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}