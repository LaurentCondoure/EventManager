namespace EventManagement.Core.Domain;

/// <summary>A user comment and rating left on an event. Stored in MongoDB.</summary>
public class EventComment
{
    /// <summary>MongoDB ObjectId (12-byte identifier, contains creation timestamp).</summary>
    public string Id { get; set; } = default!;

    /// <summary>The event this comment belongs to.</summary>
    public Guid EventId { get; set; }

    /// <summary>The user who wrote the comment.</summary>
    public Guid UserId { get; set; }

    /// <summary>Display name of the user at time of posting.</summary>
    public string UserName { get; set; } = default!;

    /// <summary>Optional comment text. Max 1000 characters.</summary>
    public string? Text { get; set; }

    /// <summary>Rating from 1 (worst) to 5 (best).</summary>
    public int Rating { get; set; }

    /// <summary>Date and time the comment was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
