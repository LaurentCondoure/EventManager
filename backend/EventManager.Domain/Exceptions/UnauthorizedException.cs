namespace EventManager.Domain.Exceptions;

/// <summary>
/// Exception thrown when authentication fails. Maps to HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedException : Exception
{
    /// <summary>
    /// Machine-readable code the frontend can branch on (e.g. <c>ACCOUNT_DEACTIVATED</c>).
    /// <c>null</c> for a generic failure — callers that must not leak which case occurred
    /// (e.g. account not found vs. wrong password) leave this unset so both responses are identical.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="UnauthorizedException"/>.
    /// </summary>
    /// <param name="message">Server-side detail for logging/traceability — never sent to the client.</param>
    /// <param name="errorCode">Optional machine-readable code included in the client response.</param>
    public UnauthorizedException(string message, string? errorCode = null) : base(message)
    {
        ErrorCode = errorCode;
    }
}
