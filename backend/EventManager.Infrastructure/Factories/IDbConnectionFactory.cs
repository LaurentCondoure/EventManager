using System.Data;

namespace EventManager.Infrastructure.Factories;

public interface IDbConnectionFactory
{
    /// <summary>
    /// Create a new DbConnection instance. Implementations must return a new connection that the caller can open/dispose.
    /// </summary>
    IDbConnection CreateConnection();
}
