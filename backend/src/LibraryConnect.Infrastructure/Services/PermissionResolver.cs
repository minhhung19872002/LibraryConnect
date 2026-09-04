using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Computes the effective permission set of a user as the union of the permissions of every group
/// they belong to, and caches it. Any change to a group, a membership or a permission assignment
/// invalidates the affected entries.
/// </summary>
public class PermissionResolver : IPermissionResolver
{
    /// <summary>Code of the seeded group whose members bypass data-scope filtering.</summary>
    public const string SystemAdministratorGroupCode = "SYS_ADMIN";

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    private readonly IApplicationDbContext _db;
    private readonly ICacheService _cache;

    public PermissionResolver(IApplicationDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyCollection<string>> GetUserPermissionsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync(
            $"{CacheKeys.Permissions}user:{userId}",
            async token => await LoadPermissionsAsync(userId, token),
            CacheTtl,
            ct);
    }

    private async Task<List<string>> LoadPermissionsAsync(Guid userId, CancellationToken ct)
    {
        var groupIds = await _db.UserGroupMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupId)
            .ToListAsync(ct);

        if (groupIds.Count == 0)
        {
            return new List<string>();
        }

        return await _db.GroupPermissions
            .Where(gp => groupIds.Contains(gp.GroupId))
            .Select(gp => gp.Permission!.Code)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<DataScopeType, IReadOnlyCollection<Guid>>> GetUserScopesAsync(Guid userId, CancellationToken ct = default)
    {
        // Được hỏi ở mọi lượt gọi của cán bộ (bộ trung gian phạm vi dữ liệu), nên đệm như quyền;
        // xoá cùng lúc với quyền khi hồ sơ người dùng đổi.
        var cached = await _cache.GetOrCreateAsync(
            $"{CacheKeys.Permissions}scopes:{userId}",
            async token =>
            {
                var rows = await _db.UserDataScopes
                    .Where(s => s.UserId == userId)
                    .Select(s => new { s.ScopeType, s.ScopeId })
                    .ToListAsync(token);

                return string.Join(";", rows.Select(r => $"{(int)r.ScopeType}:{r.ScopeId:N}"));
            },
            CacheTtl,
            ct);

        var result = new Dictionary<DataScopeType, List<Guid>>();

        foreach (var pair in (cached ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split(':');
            var type = (DataScopeType)int.Parse(parts[0]);
            var id = Guid.ParseExact(parts[1], "N");

            if (!result.TryGetValue(type, out var list))
            {
                result[type] = list = new List<Guid>();
            }

            if (!list.Contains(id))
            {
                list.Add(id);
            }
        }

        return result.ToDictionary(g => g.Key, g => (IReadOnlyCollection<Guid>)g.Value);
    }

    public async Task<bool> IsSystemAdministratorAsync(Guid userId, CancellationToken ct = default)
    {
        var cached = await _cache.GetOrCreateAsync(
            $"{CacheKeys.Permissions}admin:{userId}",
            async token => await _db.UserGroupMembers
                .AnyAsync(m => m.UserId == userId && m.Group!.Code == SystemAdministratorGroupCode, token)
                ? "1"
                : "0",
            CacheTtl,
            ct);

        return cached == "1";
    }

    public async Task InvalidateUserAsync(Guid userId, CancellationToken ct = default)
    {
        await _cache.RemoveAsync($"{CacheKeys.Permissions}user:{userId}", ct);
        await _cache.RemoveAsync($"{CacheKeys.Permissions}admin:{userId}", ct);
        await _cache.RemoveAsync($"{CacheKeys.Permissions}scopes:{userId}", ct);
    }

    public async Task InvalidateGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        var memberIds = await _db.UserGroupMembers
            .Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var memberId in memberIds)
        {
            await InvalidateUserAsync(memberId, ct);
        }
    }

    public Task InvalidateAllAsync(CancellationToken ct = default) =>
        _cache.RemoveByPrefixAsync(CacheKeys.Permissions, ct);
}
