namespace EventManager.Core.Domain;

/// <summary>Represents a registered user of the platform.</summary>
public class User
{
    /// <summary>Unique identifier of the user.</summary>
    public Guid Id { get; set; }

    /// <summary>Email address of the user. Must be unique across all users.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Display name of the user.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Date and time the account was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
