using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Features.Admin.Users;

public class UserListItemDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Position { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    /// <summary>Set while a temporary lock-out from repeated failed sign-ins is still in force.</summary>
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public IReadOnlyList<string> GroupNames { get; set; } = Array.Empty<string>();
    public DateTimeOffset CreatedAt { get; set; }
}

public class UserDetailDto : UserListItemDto
{
    public string? AvatarUrl { get; set; }
    public IReadOnlyList<Guid> GroupIds { get; set; } = Array.Empty<Guid>();
    public IReadOnlyList<UserDataScopeDto> DataScopes { get; set; } = Array.Empty<UserDataScopeDto>();
    public DateTimeOffset? PasswordChangedAt { get; set; }
    public int FailedLoginCount { get; set; }
}

public class UserDataScopeDto
{
    public DataScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public string? ScopeName { get; set; }
}

public class UserListRequest : PagedRequest
{
    public Guid? GroupId { get; set; }
    public bool? IsActive { get; set; }
    public string? Department { get; set; }
    /// <summary>True lists only accounts currently locked out.</summary>
    public bool? IsLocked { get; set; }
}

public class LoginHistoryDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

/// <summary>Result of a user Excel import (I.2), reported row by row so mistakes can be corrected.</summary>
public class UserImportResultDto
{
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int ErrorRows { get; set; }
    public List<ImportRowErrorDto> Errors { get; set; } = new();
    /// <summary>Temporary passwords generated for the newly created accounts, keyed by username.</summary>
    public Dictionary<string, string> GeneratedPasswords { get; set; } = new();
}

public class ImportRowErrorDto
{
    public int Row { get; set; }
    public string? Column { get; set; }
    public string? Value { get; set; }
    public string Message { get; set; } = string.Empty;
}
