namespace EventManager.Domain.DTOs;

/// <summary>Read model returned by the API for an event.</summary>
public record EventDto(
    /// <summary>Unique identifier of the event.</summary>
    Guid Id,
    /// <summary>Display title of the event.</summary>
    string Title,
    /// <summary>Full description of the event.</summary>
    string Description,
    /// <summary>Date and time when the event takes place (UTC).</summary>
    DateTime Date,
    /// <summary>Maximum number of seats available.</summary>
    int Capacity,
    /// <summary>Ticket price per seat.</summary>
    decimal Price,
    /// <summary>Category of the event.</summary>
    string Category,
    /// <summary>Date and time the record was created (UTC).</summary>
    DateTime CreatedAt,
    /// <summary>Date and time of the last update (UTC). Null if never updated.</summary>
    DateTime? UpdatedAt
);

/// <summary>Payload used to create a new event.</summary>
public record CreateEventRequest(
    /// <summary>Display title of the event.</summary>
    string Title,
    /// <summary>Full description of the event.</summary>
    string Description,
    /// <summary>Date and time when the event takes place (UTC).</summary>
    DateTime Date,
    /// <summary>Name and location of the venue.</summary>
    string Location,
    /// <summary>Maximum number of seats available.</summary>
    int Capacity,
    /// <summary>Ticket price per seat.</summary>
    decimal Price,
    /// <summary>Category of the event.</summary>
    string Category,
    /// <summary>Name of the artist or performing group. Optional.</summary>
    string? ArtistName
);

/// <summary>Payload used to update an existing event.</summary>
public record UpdateEventRequest(
    /// <summary>Display title of the event.</summary>
    string Title,
    /// <summary>Full description of the event.</summary>
    string Description,
    /// <summary>Date and time when the event takes place (UTC).</summary>
    DateTime Date,
    /// <summary>Name and location of the venue.</summary>
    string Location,
    /// <summary>Maximum number of seats available.</summary>
    int Capacity,
    /// <summary>Ticket price per seat.</summary>
    decimal Price,
    /// <summary>Category of the event.</summary>
    string Category,
    /// <summary>Name of the artist or performing group. Optional.</summary>
    string? ArtistName
);

/// <summary>Query parameters for paginated event listing.</summary>
public record GetEventsRequest(
    /// <summary>Page number, starting at 1.</summary>
    int Page = 1,
    /// <summary>Number of events per page.</summary>
    int PageSize = 20
);