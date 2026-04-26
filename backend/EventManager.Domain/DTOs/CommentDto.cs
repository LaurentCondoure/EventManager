namespace EventManager.Domain.DTOs;

/// <summary>
/// Read model returned by the API for a comment.
/// </summary>
public record CommentDto(
    string Id,
    Guid EventId,
    Guid UserId,
    string UserName,
    string? Text,
    int Rating,
    DateTime CreatedAt
);

/// <summary>
/// Payload used to create a new comment.
/// </summary>
public record CreateCommentInput(
    Guid UserId,
    string UserName,
    string? Text,
    int Rating
);
