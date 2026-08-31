using System.Linq.Expressions;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Admin.Users;

/// <summary>Danh sách người dùng (I.2): lọc theo nhóm, trạng thái, đơn vị; tìm theo tên/username/email.</summary>
public record GetUsersQuery(UserListRequest Request) : IRequest<PagedResult<UserListItemDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserListItemDto>>
{
    private static readonly Dictionary<string, Expression<Func<User, object?>>> Sorts = new()
    {
        ["username"] = u => u.Username,
        ["fullName"] = u => u.FullName,
        ["email"] = u => u.Email,
        ["department"] = u => u.Department,
        ["lastLoginAt"] = u => u.LastLoginAt,
        ["createdAt"] = u => u.CreatedAt
    };

    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public GetUsersQueryHandler(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var filter = request.Request;
        var keyword = filter.Keyword.Normalise();
        var now = _clock.Now;

        var query = _db.Users
            .AsNoTracking()
            .WhereIf(filter.HasKeyword(), u =>
                u.Username.ToLower().Contains(keyword) ||
                u.FullName.ToLower().Contains(keyword) ||
                (u.Email != null && u.Email.ToLower().Contains(keyword)) ||
                (u.Phone != null && u.Phone.Contains(keyword)))
            .WhereIf(filter.IsActive.HasValue, u => u.IsActive == filter.IsActive!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(filter.Department), u => u.Department == filter.Department)
            .WhereIf(filter.GroupId.HasValue, u => u.Groups.Any(g => g.GroupId == filter.GroupId!.Value && g.DeletedAt == null))
            .WhereIf(filter.IsLocked == true, u => u.LockedUntil != null && u.LockedUntil > now)
            .WhereIf(filter.IsLocked == false, u => u.LockedUntil == null || u.LockedUntil <= now)
            .ApplySort(filter, Sorts, u => u.FullName)
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                Position = u.Position,
                Department = u.Department,
                IsActive = u.IsActive,
                MustChangePassword = u.MustChangePassword,
                LockedUntil = u.LockedUntil,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt,
                GroupNames = u.Groups.Where(g => g.DeletedAt == null).Select(g => g.Group!.Name).ToList()
            });

        return await query.ToPagedResultAsync(filter, ct);
    }
}

public record GetUserByIdQuery(Guid Id) : IRequest<UserDetailDto>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetUserByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<UserDetailDto> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == request.Id)
            .Select(u => new UserDetailDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                Position = u.Position,
                Department = u.Department,
                AvatarUrl = u.AvatarUrl,
                IsActive = u.IsActive,
                MustChangePassword = u.MustChangePassword,
                LockedUntil = u.LockedUntil,
                LastLoginAt = u.LastLoginAt,
                PasswordChangedAt = u.PasswordChangedAt,
                FailedLoginCount = u.FailedLoginCount,
                CreatedAt = u.CreatedAt,
                GroupIds = u.Groups.Where(g => g.DeletedAt == null).Select(g => g.GroupId).ToList(),
                GroupNames = u.Groups.Where(g => g.DeletedAt == null).Select(g => g.Group!.Name).ToList()
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("người dùng", request.Id);

        // Scope names come from three different tables, so they are resolved after the projection.
        user.DataScopes = await ResolveScopesAsync(request.Id, ct);
        return user;
    }

    private async Task<List<UserDataScopeDto>> ResolveScopesAsync(Guid userId, CancellationToken ct)
    {
        var scopes = await _db.UserDataScopes
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => new UserDataScopeDto { ScopeType = s.ScopeType, ScopeId = s.ScopeId })
            .ToListAsync(ct);

        if (scopes.Count == 0)
        {
            return scopes;
        }

        var libraryIds = scopes.Where(s => s.ScopeType == DataScopeType.Library).Select(s => s.ScopeId).ToList();
        var warehouseIds = scopes.Where(s => s.ScopeType == DataScopeType.Warehouse).Select(s => s.ScopeId).ToList();
        var documentTypeIds = scopes.Where(s => s.ScopeType == DataScopeType.DocumentType).Select(s => s.ScopeId).ToList();

        var libraries = await _db.Libraries.AsNoTracking()
            .Where(l => libraryIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.Name, ct);
        var warehouses = await _db.Warehouses.AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, w => w.Name, ct);
        var documentTypes = await _db.DocumentTypes.AsNoTracking()
            .Where(d => documentTypeIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        foreach (var scope in scopes)
        {
            scope.ScopeName = scope.ScopeType switch
            {
                DataScopeType.Library => libraries.GetValueOrDefault(scope.ScopeId),
                DataScopeType.Warehouse => warehouses.GetValueOrDefault(scope.ScopeId),
                DataScopeType.DocumentType => documentTypes.GetValueOrDefault(scope.ScopeId),
                _ => null
            };
        }

        return scopes;
    }
}

/// <summary>Lịch sử đăng nhập của một tài khoản (I.2).</summary>
public record GetUserLoginHistoryQuery(Guid UserId, PagedRequestDefault Request)
    : IRequest<PagedResult<LoginHistoryDto>>;

public class GetUserLoginHistoryQueryHandler : IRequestHandler<GetUserLoginHistoryQuery, PagedResult<LoginHistoryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetUserLoginHistoryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<LoginHistoryDto>> Handle(GetUserLoginHistoryQuery request, CancellationToken ct)
    {
        var query = _db.LoginHistories
            .AsNoTracking()
            .Where(h => h.UserId == request.UserId)
            .OrderByDescending(h => h.OccurredAt)
            .Select(h => new LoginHistoryDto
            {
                Id = h.Id,
                Username = h.Username,
                Success = h.Success,
                FailureReason = h.FailureReason,
                Ip = h.Ip,
                UserAgent = h.UserAgent,
                OccurredAt = h.OccurredAt
            });

        return await query.ToPagedResultAsync(request.Request, ct);
    }
}

/// <summary>Danh sách đơn vị / phòng ban đang có, dùng cho bộ lọc của màn hình người dùng.</summary>
public record GetUserDepartmentsQuery : IRequest<IReadOnlyList<string>>;

public class GetUserDepartmentsQueryHandler : IRequestHandler<GetUserDepartmentsQuery, IReadOnlyList<string>>
{
    private readonly IApplicationDbContext _db;

    public GetUserDepartmentsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<string>> Handle(GetUserDepartmentsQuery request, CancellationToken ct) =>
        await _db.Users
            .AsNoTracking()
            .Where(u => u.Department != null && u.Department != "")
            .Select(u => u.Department!)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync(ct);
}
