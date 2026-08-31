using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>
/// The identity behind the current request. Implemented over the JWT claims by the API layer, so
/// handlers never touch HttpContext directly.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? ReaderId { get; }
    string? Username { get; }
    string? FullName { get; }
    bool IsAuthenticated { get; }
    /// <summary>True for the seeded system administrator group, which bypasses scope filtering.</summary>
    bool IsSystemAdministrator { get; }
    string? Ip { get; }
    string? UserAgent { get; }

    IReadOnlyCollection<string> Permissions { get; }

    bool HasPermission(string permissionCode);

    /// <summary>
    /// Ids the user may operate on for a given scope dimension. An empty set means "unrestricted",
    /// which is how a user with no explicit scope rows is treated.
    /// </summary>
    IReadOnlyCollection<Guid> ScopeIds(DataScopeType scopeType);

    bool IsInScope(DataScopeType scopeType, Guid id);
}
