namespace EventManager.Infrastructure.Identity;

public sealed class PasswordHistory
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string HashedPassword { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}