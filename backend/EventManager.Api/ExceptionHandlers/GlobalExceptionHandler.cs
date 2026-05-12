using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Api.ExceptionHandlers;

/// <summary>
/// Catch-all exception handler. Invoked when no prior handler returned true.
/// In production: returns a generic HTTP 500 response without leaking internal details.
/// In development: enriches the response with exception type, message and stack trace.
/// </summary>
/// <param name="logger">The logger instance.</param>
/// <param name="environment">Used to detect whether the app runs in development mode.</param>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var requestId = httpContext.TraceIdentifier;

        logger.LogError(
            exception,
            "Unhandled exception [{RequestId}] on {Method} {Path}",
            requestId,
            httpContext.Request.Method,
            httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status   = StatusCodes.Status500InternalServerError,
            Title    = "An unexpected error occurred",
            Detail   = "An unexpected error occurred. Please try again later.",
            Instance = httpContext.Request.Path,
            Extensions = { ["requestId"] = requestId }
        };

        // In dev environment only expose technical failure to facilitate debug.
        if (environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"]    = exception.GetType().FullName;
            problemDetails.Extensions["exceptionMessage"] = exception.Message;
            problemDetails.Extensions["stackTrace"]       = exception.StackTrace;
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
