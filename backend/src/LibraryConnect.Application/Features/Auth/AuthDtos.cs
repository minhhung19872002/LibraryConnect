namespace LibraryConnect.Application.Features.Auth;

/// <summary>Payload returned by every successful login and refresh.</summary>
public class AuthResultDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    public bool MustChangePassword { get; set; }
    public AuthUserDto User { get; set; } = new();
}

public class AuthUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    /// <summary>True when the account is a reader (OPAC / mobile) rather than a staff user.</summary>
    public bool IsReader { get; set; }
    public IReadOnlyList<string> Groups { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
    public IReadOnlyList<DataScopeDto> DataScopes { get; set; } = Array.Empty<DataScopeDto>();
}

public class DataScopeDto
{
    public string ScopeType { get; set; } = string.Empty;
    public Guid ScopeId { get; set; }
    public string? ScopeName { get; set; }
}
