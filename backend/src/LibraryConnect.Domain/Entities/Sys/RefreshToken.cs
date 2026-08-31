using LibraryConnect.Domain.Common;

namespace LibraryConnect.Domain.Entities.Sys;

/// <summary>
/// Refresh tokens are stored hashed so a database leak cannot be replayed. Both staff and readers
/// use the same table; exactly one of UserId / ReaderId is populated.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public Guid? ReaderId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public string? CreatedIp { get; set; }
    public string? UserAgent { get; set; }

    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}
