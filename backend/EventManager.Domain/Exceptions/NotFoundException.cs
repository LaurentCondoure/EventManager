namespace EventManager.Domain.Exceptions;

/// <summary>
/// Exception thrown when a requested resource does not exist in the data store.
/// Maps to HTTP 404 Not Found via <see cref="EventManager.Api.Middleware.ExceptionMiddleware"/>.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="NotFoundException"/>.
    /// </summary>
    /// <param name="entityName">The name of the entity type that was not found (e.g. "Event").</param>
    /// <param name="key">The key used to look up the entity.</param>
    public NotFoundException(string entityName, object key)
        : base($"{entityName} '{key}' was not found.")
    {
    }
}
