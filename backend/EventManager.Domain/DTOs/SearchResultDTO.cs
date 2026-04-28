using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Domain.DTOs
{
    /// <summary>Read model returned by the API for an event.</summary>
    public record SearchResultDto(
        /// <summary>Unique identifier of the event.</summary>
        Guid Id,
        /// <summary>Display title of the event.</summary>
        string Title,
        /// <summary>Full description of the event.</summary>
        string Description,
        /// <summary>Date and time when the event takes place (UTC).</summary>
        DateTime Date,
        /// <summary>Name and location of the venue (city, hall).</summary>
        string Location,
        /// <summary>Ticket price per seat.</summary>
        decimal Price,
        /// <summary>Category of the event.</summary>
        string Category,
        /// <summary>Name of the artist or performing group at the event.</summary>
        string? ArtistName
    );
}
