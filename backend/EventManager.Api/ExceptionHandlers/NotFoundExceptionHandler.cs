using EventManager.Domain.Exceptions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Api.ExceptionHandlers;

/// <summary>
/// Handles <see cref="NotFoundException"/> and returns a standardized HTTP 404 ProblemDetails response.
/// Registered first in the handler chain — returns false for any other exception type,
/// allowing the next handler to take over.
/// </summary>
/// <param name="logger">The logger instance.</param>
public sealed class NotFoundExceptionHandler(ILogger<NotFoundExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<NotFoundExceptionHandler> _logger = logger;

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException notFoundException)
            return false;

        var requestId = httpContext.TraceIdentifier;

        _logger.LogWarning(
            "Resource not found [{RequestId}] on {Method} {Path}: {Message}",
            requestId,
            httpContext.Request.Method,
            httpContext.Request.Path,
            notFoundException.Message);

        var problemDetails = new ProblemDetails
        {
            Status   = StatusCodes.Status404NotFound,
            Title    = "Resource not found",
            Detail   = "The requested resource does not exist.",
            Instance = httpContext.Request.Path,
            Extensions = { ["requestId"] = requestId }
        };

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
