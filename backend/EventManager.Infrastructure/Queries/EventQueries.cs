namespace EventManager.Infrastructure.Queries;

/// <summary>Centralized storage for all SQL queries related to event management.</summary>
public class EventQueries
{
    /// <summary>SQL query to retrieve a paginated list of upcoming events.</summary>
    internal const string GetAll = @"
            SELECT Id, Title, Description, Date, Location, Capacity, Price, Category, ArtistName, CreatedAt, UpdatedAt
            FROM Events
            WHERE Date >= SYSUTCDATETIME()
            ORDER BY Date ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            ";

    /// <summary>SQL query to retrieve a single event by its unique identifier.</summary>
    internal const string GetById = @"
            SELECT Id, Title, Description, Date, Location, Capacity, Price, Category, ArtistName, CreatedAt, UpdatedAt
            FROM Events
            WHERE Id = @Id
            ";
    
    /// <summary>SQL query to create a new event record in the database.</summary>
    internal const string Create = @"
            INSERT INTO Events (Id, Title, Description, Date, Location , Capacity, Price, Category, ArtistName, CreatedAt)
            VALUES (@Id, @Title, @Description, @Date, @Location, @Capacity, @Price, @Category, @ArtistName, @CreatedAt)
            ";

    /// <summary>SQL query to update an existing event record.</summary>
    internal const string Update = @"
            UPDATE Events
            SET Title       = @Title,
                Description = @Description,
                Date        = @Date,
                Location    = @Location,
                Capacity    = @Capacity,
                Price       = @Price,
                Category    = @Category,
                ArtistName  = @ArtistName,
                UpdatedAt   = @UpdatedAt
            WHERE Id = @Id
            ";

    /// <summary>SQL query to delete an event record by its ID.</summary>
    internal const string Delete = @"DELETE FROM Events WHERE Id = @Id";
}
