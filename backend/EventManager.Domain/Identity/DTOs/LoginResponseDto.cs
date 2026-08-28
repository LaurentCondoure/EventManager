namespace EventManager.Domain.Identity.DTOs;

/// <summary>
/// JSON body returned by <c>POST /auth/login</c> on success. The token pair itself never
/// appears here — it travels exclusively as httpOnly cookies (ADR-014).
/// </summary>
public record LoginResponseDto(
    /// <summary>The authenticated user's role.</summary>
    string Role,
    /// <summary>Whether the frontend must redirect to the password reset screen.</summary>
    bool MustResetPassword
);
