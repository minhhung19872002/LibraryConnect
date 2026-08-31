using System.Linq.Expressions;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Domain.Entities.Sys;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Admin.UserGroups;

/// <summary>Danh sách nhóm người dùng (I.1): tìm kiếm, lọc theo trạng thái, phân trang.</summary>
public record GetUserGroupsQuery(UserGroupListRequest Request) : IRequest<PagedResult<UserGroupListItemDto>>;

public class GetUserGroupsQueryHandler : IRequestHandler<GetUserGroupsQuery, PagedResult<UserGroupListItemDto>>
{
    private static readonly Dictionary<string, Expression<Func<UserGroup, object?>>> Sorts = new()
    {
        ["code"] = g => g.Code,
        ["name"] = g => g.Name,
        ["createdAt"] = g => g.CreatedAt
    };

    private readonly IApplicationDbContext _db;

    public GetUserGroupsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<UserGroupListItemDto>> Handle(GetUserGroupsQuery request, CancellationToken ct)
    {
        var filter = request.Request;
        var keyword = filter.Keyword.Normalise();

        var query = _db.UserGroups
            .AsNoTracking()
            .WhereIf(filter.HasKeyword(), g =>
                g.Code.ToLower().Contains(keyword) ||
                g.Name.ToLower().Contains(keyword) ||
                (g.Description != null && g.Description.ToLower().Contains(keyword)))
            .WhereIf(filter.IsActive.HasValue, g => g.IsActive == filter.IsActive!.Value)
            .WhereIf(filter.IsSystem.HasValue, g => g.IsSystem == filter.IsSystem!.Value)
            .ApplySort(filter, Sorts, g => g.Name)
            .Select(g => new UserGroupListItemDto
            {
                Id = g.Id,
                Code = g.Code,
                Name = g.Name,
                Description = g.Description,
                IsSystem = g.IsSystem,
                IsActive = g.IsActive,
                CreatedAt = g.CreatedAt,
                MemberCount = g.Members.Count(m => m.DeletedAt == null),
                PermissionCount = g.Permissions.Count(p => p.DeletedAt == null)
            });

        return await query.ToPagedResultAsync(filter, ct);
    }
}

public record GetUserGroupByIdQuery(Guid Id) : IRequest<UserGroupDetailDto>;

public class GetUserGroupByIdQueryHandler : IRequestHandler<GetUserGroupByIdQuery, UserGroupDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetUserGroupByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<UserGroupDetailDto> Handle(GetUserGroupByIdQuery request, CancellationToken ct)
    {
        var group = await _db.UserGroups
            .AsNoTracking()
            .Where(g => g.Id == request.Id)
            .Select(g => new UserGroupDetailDto
            {
                Id = g.Id,
                Code = g.Code,
                Name = g.Name,
                Description = g.Description,
                IsSystem = g.IsSystem,
                IsActive = g.IsActive,
                CreatedAt = g.CreatedAt,
                MemberCount = g.Members.Count(m => m.DeletedAt == null),
                PermissionCount = g.Permissions.Count(p => p.DeletedAt == null),
                PermissionCodes = g.Permissions
                    .Where(p => p.DeletedAt == null)
                    .Select(p => p.Permission!.Code)
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        return group ?? throw new NotFoundException("nhóm người dùng", request.Id);
    }
}

/// <summary>
/// Cây quyền của một nhóm (I.1). The tree is built from sys.permissions rather than from a hardcoded
/// list, so a permission added in a later release appears in the UI automatically.
/// </summary>
public record GetGroupPermissionsQuery(Guid GroupId) : IRequest<GroupPermissionsDto>;

public class GetGroupPermissionsQueryHandler : IRequestHandler<GetGroupPermissionsQuery, GroupPermissionsDto>
{
    private readonly IApplicationDbContext _db;

    public GetGroupPermissionsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<GroupPermissionsDto> Handle(GetGroupPermissionsQuery request, CancellationToken ct)
    {
        var group = await _db.UserGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, ct)
            ?? throw new NotFoundException("nhóm người dùng", request.GroupId);

        var permissions = await _db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .Select(p => new { p.Code, p.Module, p.Group, p.Name })
            .ToListAsync(ct);

        var granted = await _db.GroupPermissions
            .AsNoTracking()
            .Where(gp => gp.GroupId == request.GroupId)
            .Select(gp => gp.Permission!.Code)
            .ToListAsync(ct);

        var tree = permissions
            .GroupBy(p => p.Module)
            .Select(moduleGroup => new PermissionTreeNodeDto
            {
                Key = $"module:{moduleGroup.Key}",
                Title = moduleGroup.Key,
                Children = moduleGroup
                    .GroupBy(p => p.Group)
                    .Select(functionGroup => new PermissionTreeNodeDto
                    {
                        Key = $"group:{moduleGroup.Key}/{functionGroup.Key}",
                        Title = functionGroup.Key,
                        Children = functionGroup
                            .Select(p => new PermissionTreeNodeDto
                            {
                                Key = p.Code,
                                Code = p.Code,
                                Title = p.Name
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();

        return new GroupPermissionsDto
        {
            GroupId = group.Id,
            GroupName = group.Name,
            IsSystem = group.IsSystem,
            Tree = tree,
            GrantedCodes = granted
        };
    }
}

/// <summary>Thành viên của nhóm (I.1).</summary>
public record GetGroupMembersQuery(Guid GroupId, PagedRequestDefault Request) : IRequest<PagedResult<GroupMemberDto>>;

public class GetGroupMembersQueryHandler : IRequestHandler<GetGroupMembersQuery, PagedResult<GroupMemberDto>>
{
    private static readonly Dictionary<string, Expression<Func<UserGroupMember, object?>>> Sorts = new()
    {
        ["username"] = m => m.User!.Username,
        ["fullName"] = m => m.User!.FullName
    };

    private readonly IApplicationDbContext _db;

    public GetGroupMembersQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<GroupMemberDto>> Handle(GetGroupMembersQuery request, CancellationToken ct)
    {
        var keyword = request.Request.Keyword.Normalise();

        var query = _db.UserGroupMembers
            .AsNoTracking()
            .Where(m => m.GroupId == request.GroupId)
            .WhereIf(request.Request.HasKeyword(), m =>
                m.User!.Username.ToLower().Contains(keyword) ||
                m.User!.FullName.ToLower().Contains(keyword))
            .ApplySort(request.Request, Sorts, m => m.User!.FullName)
            .Select(m => new GroupMemberDto
            {
                UserId = m.UserId,
                Username = m.User!.Username,
                FullName = m.User!.FullName,
                Email = m.User!.Email,
                Department = m.User!.Department,
                IsActive = m.User!.IsActive
            });

        return await query.ToPagedResultAsync(request.Request, ct);
    }
}
