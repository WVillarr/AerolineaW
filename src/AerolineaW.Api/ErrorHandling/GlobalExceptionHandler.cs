using AerolineaW.Application.Common;
using AerolineaW.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AerolineaW.Api.ErrorHandling;

/// <summary>
/// The single place an exception becomes an HTTP response. Every controller action used to
/// wrap itself in try/catch and return <c>StatusCode(500, e)</c> — serializing the raw
/// exception to the client; this replaces all of that with one RFC 7807 ProblemDetails
/// mapping, and never leaks exception details for anything it doesn't recognize.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            DomainException => (StatusCodes.Status409Conflict, "Business Rule Violation"),

            // Includes exhausted booking retries and conflicting lifecycle mutations.
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency Conflict"),
            PersistenceConflictException => (StatusCodes.Status409Conflict, "Persistence Conflict"),

            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid Input"),
            _ => (StatusCodes.Status500InternalServerError, "Server Error"),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception switch
            {
                DbUpdateConcurrencyException => "The record changed during this operation. Reload it and try again.",
                _ when statusCode == StatusCodes.Status500InternalServerError => "An unexpected error occurred.",
                _ => exception.Message,
            },
            Instance = httpContext.Request.Path,
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json", cancellationToken);
        return true;
    }
}
