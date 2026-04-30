namespace EventManager.Infrastructure.Options;

/// <summary>
/// Strongly-typed configuration for the fixed-window rate limiter.
/// </summary>
public sealed class RateLimiterOptions
{
    public const string SectionName = "RateLimiter";

    /// <summary>Maximum number of requests permitted per window.</summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>Duration of the rate-limit window, in minutes.</summary>
    public int WindowMinutes { get; set; } = 1;

    /// <summary>
    /// Maximum number of requests queued when the limit is reached. 0 means no queuing — excess requests are rejected immediately.
    /// </summary>
    public int QueueLimit { get; set; } = 0;
}