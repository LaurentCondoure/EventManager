namespace EventManager.Domain.Exceptions;

/// <summary>
/// Exception thrown when a request is malformed or contains invalid data that cannot be parsed.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public class BadRequestException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="BadRequestException"/> with a detail message.
    /// </summary>
    /// <param name="message">A description of why the request is invalid.</param>
    public BadRequestException(string message) : base(message)
    {
    }
}
