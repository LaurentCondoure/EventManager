namespace EventManager.Domain.Identity.DTOs;

/// <summary>Outcome of a password verification attempt against a user's credentials.</summary>
public enum PasswordVerificationResult
{
    /// <summary>The password matches and the account is not locked out.</summary>
    Success,

    /// <summary>The password does not match, or the account does not exist.</summary>
    Failed,

    /// <summary>The account is locked out following repeated failed attempts.</summary>
    LockedOut
}
