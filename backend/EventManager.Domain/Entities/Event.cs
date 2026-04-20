namespace EventManagement.Domain.Entities;

/// <summary>Represents a cultural event (concert, show, exhibition).</summary>
public class Event
{
    /// <summary>Unique identifier of the event.</summary>
    public Guid Id { get; set; }

    /// <summary>Display title of the event.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Full description of the event.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Date and time when the event takes place (UTC).</summary>
    public DateTime Date { get; set; }

    /// <summary>Name and location of the venue (city, hall).</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>Maximum number of seats available.</summary>
    public int Capacity { get; set; }

    /// <summary>Ticket price per seat.</summary>
    public decimal Price { get; set; }

    /// <summary>Category of the event (e.g. Concert, Théâtre, Exposition).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Name of the artist or performing group. Optional.</summary>
    public string? ArtistName { get; set; }

    /// <summary>Date and time the record was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Date and time of the last update (UTC). Null if never updated.</summary>
    public DateTime? UpdatedAt { get; set; }
}
