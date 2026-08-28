namespace EventManager.Domain.Identity.Constants;

/// <summary>
/// Machine-readable error codes returned to the frontend alongside a 4xx auth response,
/// distinct from the message shown to the user (ADR-014).
/// </summary>
public enum AuthenticationErrorCode
{
    /// <summary>The account exists and the credentials may be valid, but the account is deactivated.</summary>
    AccountDeactivated
}

/// <summary>Maps <see cref="AuthenticationErrorCode"/> to the exact wire string the frontend matches on.</summary>
public static class AuthenticationErrorCodes
{
    public static string ToErrorCode(this AuthenticationErrorCode code) => code switch
    {
        AuthenticationErrorCode.AccountDeactivated => "ACCOUNT_DEACTIVATED",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };
}
