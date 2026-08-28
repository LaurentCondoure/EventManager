namespace EventManager.Domain.Identity.DTOs;

/// <summary>Payload for <c>POST /auth/login</c>.</summary>
public record LoginInput(
    /// <summary>Account email address.</summary>
    string Email,
    /// <summary>Account password, in clear text over TLS.</summary>
    string Password
);
