using LibraryConnect.Domain.Common;

namespace LibraryConnect.Domain.Entities.Sys;

/// <summary>Staff account (schema <c>sys.users</c>). Readers authenticate separately, see rdr.readers.</summary>
public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Position { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTimeOffset? PasswordChangedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public string? AvatarUrl { get; set; }

    public ICollection<UserGroupMember> Groups { get; set; } = new List<UserGroupMember>();
    public ICollection<UserDataScope> DataScopes { get; set; } = new List<UserDataScope>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
