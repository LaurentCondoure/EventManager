using EventManager.Domain.Exceptions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Api.ExceptionHandlers;

/// <summary>
/// Handles <see cref="UnauthorizedException"/> and returns a standardized HTTP 401 ProblemDetails
/// response. When the exception carries no <c>ErrorCode</c>, the response body is a fixed generic
/// shape — this is what keeps "account not found" and "wrong password" indistinguishable to the
/// client (ADR-014). The distinguishing detail lives only in the server-side log line.
/// </summary>
/// <param name="logger">The logger instance.</param>
public sealed class UnauthorizedExceptionHandler(ILogger<UnauthorizedExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc/>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not UnauthorizedException unauthorizedException)
            return false;

        var requestId = httpContext.TraceIdentifier;
        var hasErrorCode = unauthorizedException.ErrorCode is not null;

        logger.LogWarning(
            "Unauthorized [{RequestId}] on {Method} {Path}: {Message}",
            requestId,
            httpContext.Request.Method,
            httpContext.Request.Path,
            unauthorizedException.Message);

        var problemDetails = new ProblemDetails
        {
            Status   = StatusCodes.Status401Unauthorized,
            Title    = hasErrorCode ? "Account deactivated" : "Unauthorized",
            Detail   = hasErrorCode ? "This account has been deactivated." : "Invalid credentials.",
            Instance = httpContext.Request.Path,
            Extensions = { ["requestId"] = requestId }
        };

        if (hasErrorCode)
            problemDetails.Extensions["errorCode"] = unauthorizedException.ErrorCode;

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
