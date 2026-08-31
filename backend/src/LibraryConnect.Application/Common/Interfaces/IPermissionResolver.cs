using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>
/// Resolves the effective permission set of a staff user (union of the permissions of every group
/// they belong to) and their data scopes. Results are cached in Redis and invalidated whenever a
/// group, a membership or a permission assignment changes.
/// </summary>
public interface IPermissionResolver
{
    Task<IReadOnlyCollection<string>> GetUserPermissionsAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<DataScopeType, IReadOnlyCollection<Guid>>> GetUserScopesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>True when the user belongs to a group flagged as the system administrator group.</summary>
    Task<bool> IsSystemAdministratorAsync(Guid userId, CancellationToken ct = default);

    Task InvalidateUserAsync(Guid userId, CancellationToken ct = default);

    Task InvalidateGroupAsync(Guid groupId, CancellationToken ct = default);

    Task InvalidateAllAsync(CancellationToken ct = default);
}
