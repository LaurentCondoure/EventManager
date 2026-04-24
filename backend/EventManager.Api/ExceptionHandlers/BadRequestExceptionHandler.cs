using EventManager.Domain.Exceptions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Api.ExceptionHandlers;

/// <summary>
/// Handles <see cref="BadRequestException"/> and returns a standardized HTTP 400 ProblemDetails response.
/// </summary>
/// <param name="logger">The logger instance.</param>
public sealed class BadRequestExceptionHandler(ILogger<BadRequestExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<BadRequestExceptionHandler> _logger = logger;

    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BadRequestException badRequestException)
            return false;

        var requestId = httpContext.TraceIdentifier;

        _logger.LogWarning(
            "Bad request [{RequestId}] on {Method} {Path}: {Message}",
            requestId,
            httpContext.Request.Method,
            httpContext.Request.Path,
            badRequestException.Message);

        var problemDetails = new ProblemDetails
        {
            Status   = StatusCodes.Status400BadRequest,
            Title    = "Invalid request format",
            Detail   = "The request could not be understood by the server.",
            Instance = httpContext.Request.Path,
            Extensions = { ["requestId"] = requestId }
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
