namespace EventManager.Api.Auth.Authentication;

/// <summary>Names of the httpOnly cookies carrying the JWT access and refresh tokens (ADR-014).</summary>
public static class AuthCookieNames
{
    /// <summary>Cookie holding the short-lived JWT access token.</summary>
    public const string AccessToken = "access_token";

    /// <summary>Cookie holding the session-duration refresh token.</summary>
    public const string RefreshToken = "refresh_token";
}

/// <summary>Builds the <see cref="CookieOptions"/> shared by every endpoint that issues or clears auth cookies.</summary>
public static class AuthCookieOptions
{
    /// <summary>
    /// Standard options for an auth cookie: <c>HttpOnly</c>, <c>Secure</c>, <c>SameSite=Strict</c> (ADR-014).
    /// </summary>
    /// <param name="expires">Absolute expiration of the cookie.</param>
    public static CookieOptions Create(DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure   = true,
        SameSite = SameSiteMode.Strict,
        Expires  = expires,
        Path     = "/"
    };
}
