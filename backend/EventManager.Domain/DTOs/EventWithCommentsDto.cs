namespace EventManager.Domain.DTOs;

/// <summary>
/// An event combined with its comments — aggregates SQL Server and MongoDB data.
/// </summary>
public record EventWithCommentsDto(
    EventDto Event,
    IEnumerable<CommentDto> Comments
);
