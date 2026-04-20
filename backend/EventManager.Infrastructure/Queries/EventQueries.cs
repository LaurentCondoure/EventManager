namespace EventManagement.Infrastructure.Queries;

/// <summary>Centralized storage for all SQL queries related to event management.</summary>
public class EventQueries
{
    /// <summary>SQL query to retrieve a paginated list of upcoming events.</summary>
    internal const string GetAll = @"
            SELECT Id, Title, Description, Date, Capacity, Price, Category, CreatedAt, UpdatedAt
            FROM Events
            WHERE Date >= SYSUTCDATETIME()
            ORDER BY Date ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            ";

    /// <summary>SQL query to retrieve a single event by its unique identifier.</summary>
    internal const string GetById = @"
            SELECT Id, Title, Description, Date, Capacity, Price, Category, CreatedAt, UpdatedAt
            FROM Events
            WHERE Id = @Id
            ";
    
    /// <summary>SQL query to create a new event record in the database.</summary>
    internal const string Create = @"
            INSERT INTO Events (Id, Title, Description, Date, Capacity, Price, Category, CreatedAt)
            VALUES (@Id, @Title, @Description, @Date, @Capacity, @Price, @Category, @CreatedAt)
            ";
    
}
