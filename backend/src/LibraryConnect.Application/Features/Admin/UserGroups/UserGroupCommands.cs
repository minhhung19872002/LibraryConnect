using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Sys;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Admin.UserGroups;

// ---------------------------------------------------------------------------
// Tạo nhóm
// ---------------------------------------------------------------------------

public record CreateUserGroupCommand(string Code, string Name, string? Description, bool IsActive = true)
    : IRequest<Guid>;

public class CreateUserGroupCommandValidator : AbstractValidator<CreateUserGroupCommand>
{
    public CreateUserGroupCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Vui lòng nhập mã nhóm.")
            .MaximumLength(50).WithMessage("Mã nhóm tối đa 50 ký tự.")
            .Matches("^[A-Za-z0-9_]+$").WithMessage("Mã nhóm chỉ gồm chữ cái, chữ số và dấu gạch dưới.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Vui lòng nhập tên nhóm.")
            .MaximumLength(200).WithMessage("Tên nhóm tối đa 200 ký tự.");

        RuleFor(x => x.Description).MaximumLength(500).WithMessage("Mô tả tối đa 500 ký tự.");
    }
}

public class CreateUserGroupCommandHandler : IRequestHandler<CreateUserGroupCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateUserGroupCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateUserGroupCommand request, CancellationToken ct)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        if (await _db.UserGroups.AnyAsync(g => g.Code == code, ct))
        {
            throw new ConflictException($"Mã nhóm '{code}' đã tồn tại.");
        }

        var group = new UserGroup
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            IsSystem = false
        };

        _db.UserGroups.Add(group);
        await _db.SaveChangesAsync(ct);

        return group.Id;
    }
}

// ---------------------------------------------------------------------------
// Sửa nhóm
// ---------------------------------------------------------------------------

public record UpdateUserGroupCommand(Guid Id, string Name, string? Description, bool IsActive) : IRequest<Unit>;

public class UpdateUserGroupCommandValidator : AbstractValidator<UpdateUserGroupCommand>
{
    public UpdateUserGroupCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Vui lòng nhập tên nhóm.")
            .MaximumLength(200).WithMessage("Tên nhóm tối đa 200 ký tự.");

        RuleFor(x => x.Description).MaximumLength(500).WithMessage("Mô tả tối đa 500 ký tự.");
    }
}

public class UpdateUserGroupCommandHandler : IRequestHandler<UpdateUserGroupCommand, Unit>
{
    private readonly IApplicationDbContext _db;

    public UpdateUserGroupCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateUserGroupCommand request, CancellationToken ct)
    {
        var group = await _db.UserGroups.FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new NotFoundException("nhóm người dùng", request.Id);

        // The code of a seeded group is referenced from code (e.g. SYS_ADMIN), so only the display
        // fields are editable; the code itself is never editable for any group.
        group.Name = request.Name.Trim();
        group.Description = request.Description?.Trim();

        if (group.IsSystem && !request.IsActive)
        {
            throw new ConflictException("Không thể vô hiệu hóa nhóm hệ thống.");
        }

        group.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ---------------------------------------------------------------------------
// Xóa nhóm
// ---------------------------------------------------------------------------

public record DeleteUserGroupCommand(Guid Id, string? Reason) : IRequest<Unit>;

public class DeleteUserGroupCommandHandler : IRequestHandler<DeleteUserGroupCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IPermissionResolver _permissions;

    public DeleteUserGroupCommandHandler(IApplicationDbContext db, IPermissionResolver permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task<Unit> Handle(DeleteUserGroupCommand request, CancellationToken ct)
    {
        var group = await _db.UserGroups.FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new NotFoundException("nhóm người dùng", request.Id);

        if (group.IsSystem)
        {
            throw new ConflictException("Không thể xóa nhóm hệ thống.");
        }

        var memberCount = await _db.UserGroupMembers.CountAsync(m => m.GroupId == group.Id, ct);
        if (memberCount > 0)
        {
            throw new ConflictException(
                $"Nhóm đang có {memberCount} thành viên. Vui lòng chuyển thành viên sang nhóm khác trước khi xóa.");
        }

        // Soft delete: the interceptor converts Remove into a DeletedAt stamp.
        _db.GroupPermissions.RemoveRange(
            await _db.GroupPermissions.Where(gp => gp.GroupId == group.Id).ToListAsync(ct));
        _db.UserGroups.Remove(group);

        await _db.SaveChangesAsync(ct);
        await _permissions.InvalidateGroupAsync(group.Id, ct);

        return Unit.Value;
    }
}

// ---------------------------------------------------------------------------
// Gán quyền cho nhóm
// ---------------------------------------------------------------------------

public record UpdateGroupPermissionsCommand(Guid GroupId, IReadOnlyList<string> PermissionCodes) : IRequest<Unit>;

public class UpdateGroupPermissionsCommandHandler : IRequestHandler<UpdateGroupPermissionsCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IPermissionResolver _permissions;
    private readonly IAuditService _audit;

    public UpdateGroupPermissionsCommandHandler(
        IApplicationDbContext db, IPermissionResolver permissions, IAuditService audit)
    {
        _db = db;
        _permissions = permissions;
        _audit = audit;
    }

    public async Task<Unit> Handle(UpdateGroupPermissionsCommand request, CancellationToken ct)
    {
        var group = await _db.UserGroups.FirstOrDefaultAsync(g => g.Id == request.GroupId, ct)
            ?? throw new NotFoundException("nhóm người dùng", request.GroupId);

        // The tree sends parent keys ("module:...") alongside the leaves; only real codes count.
        var requested = request.PermissionCodes
            .Where(code => !code.Contains(':'))
            .Distinct()
            .ToList();

        var permissionIds = await _db.Permissions
            .Where(p => requested.Contains(p.Code))
            .Select(p => new { p.Id, p.Code })
            .ToListAsync(ct);

        var unknown = requested.Except(permissionIds.Select(p => p.Code)).ToList();
        if (unknown.Count > 0)
        {
            throw new ConflictException($"Mã quyền không tồn tại: {string.Join(", ", unknown)}");
        }

        var current = await _db.GroupPermissions
            .Where(gp => gp.GroupId == group.Id)
            .ToListAsync(ct);

        var currentIds = current.Select(gp => gp.PermissionId).ToHashSet();
        var targetIds = permissionIds.Select(p => p.Id).ToHashSet();

        var toRemove = current.Where(gp => !targetIds.Contains(gp.PermissionId)).ToList();
        var toAdd = targetIds.Except(currentIds)
            .Select(id => new GroupPermission { GroupId = group.Id, PermissionId = id })
            .ToList();

        if (toRemove.Count == 0 && toAdd.Count == 0)
        {
            return Unit.Value;
        }

        _db.GroupPermissions.RemoveRange(toRemove);
        _db.GroupPermissions.AddRange(toAdd);
        await _db.SaveChangesAsync(ct);

        // Members carry their permissions in the access token, so the cache must be dropped for
        // every one of them; the change takes full effect within one access-token lifetime.
        await _permissions.InvalidateGroupAsync(group.Id, ct);

        await _audit.LogAsync(
            Domain.Enums.AuditAction.PermissionChange,
            nameof(UserGroup),
            group.Id.ToString(),
            group.Name,
            newValue: new { Added = toAdd.Count, Removed = toRemove.Count, Total = targetIds.Count },
            message: $"Cập nhật quyền nhóm '{group.Name}': thêm {toAdd.Count}, bỏ {toRemove.Count}",
            ct: ct);

        return Unit.Value;
    }
}

// ---------------------------------------------------------------------------
// Sao chép quyền từ nhóm khác
// ---------------------------------------------------------------------------

public record CloneGroupPermissionsCommand(Guid TargetGroupId, Guid SourceGroupId, bool Replace = true)
    : IRequest<int>;

public class CloneGroupPermissionsCommandHandler : IRequestHandler<CloneGroupPermissionsCommand, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ISender _sender;

    public CloneGroupPermissionsCommandHandler(IApplicationDbContext db, ISender sender)
    {
        _db = db;
        _sender = sender;
    }

    public async Task<int> Handle(CloneGroupPermissionsCommand request, CancellationToken ct)
    {
        if (request.TargetGroupId == request.SourceGroupId)
        {
            throw new ConflictException("Nhóm nguồn và nhóm đích phải khác nhau.");
        }

        var sourceCodes = await _db.GroupPermissions
            .Where(gp => gp.GroupId == request.SourceGroupId)
            .Select(gp => gp.Permission!.Code)
            .ToListAsync(ct);

        if (sourceCodes.Count == 0)
        {
            throw new ConflictException("Nhóm nguồn chưa được cấp quyền nào.");
        }

        var finalCodes = sourceCodes;

        if (!request.Replace)
        {
            // Merge mode: keep what the target already had and add the source's rights on top.
            var existing = await _db.GroupPermissions
                .Where(gp => gp.GroupId == request.TargetGroupId)
                .Select(gp => gp.Permission!.Code)
                .ToListAsync(ct);

            finalCodes = sourceCodes.Union(existing).ToList();
        }

        await _sender.Send(new UpdateGroupPermissionsCommand(request.TargetGroupId, finalCodes), ct);
        return finalCodes.Count;
    }
}

// ---------------------------------------------------------------------------
// Thêm / bớt thành viên hàng loạt
// ---------------------------------------------------------------------------

public record UpdateGroupMembersCommand(Guid GroupId, IReadOnlyList<Guid> AddUserIds, IReadOnlyList<Guid> RemoveUserIds)
    : IRequest<Unit>;

public class UpdateGroupMembersCommandHandler : IRequestHandler<UpdateGroupMembersCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IPermissionResolver _permissions;

    public UpdateGroupMembersCommandHandler(IApplicationDbContext db, IPermissionResolver permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task<Unit> Handle(UpdateGroupMembersCommand request, CancellationToken ct)
    {
        var group = await _db.UserGroups.FirstOrDefaultAsync(g => g.Id == request.GroupId, ct)
            ?? throw new NotFoundException("nhóm người dùng", request.GroupId);

        if (request.RemoveUserIds.Count > 0)
        {
            var removals = await _db.UserGroupMembers
                .Where(m => m.GroupId == group.Id && request.RemoveUserIds.Contains(m.UserId))
                .ToListAsync(ct);

            _db.UserGroupMembers.RemoveRange(removals);
        }

        if (request.AddUserIds.Count > 0)
        {
            var already = await _db.UserGroupMembers
                .Where(m => m.GroupId == group.Id && request.AddUserIds.Contains(m.UserId))
                .Select(m => m.UserId)
                .ToListAsync(ct);

            var existingUsers = await _db.Users
                .Where(u => request.AddUserIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync(ct);

            var additions = existingUsers
                .Except(already)
                .Select(userId => new UserGroupMember { GroupId = group.Id, UserId = userId })
                .ToList();

            _db.UserGroupMembers.AddRange(additions);
        }

        await _db.SaveChangesAsync(ct);

        foreach (var userId in request.AddUserIds.Concat(request.RemoveUserIds).Distinct())
        {
            await _permissions.InvalidateUserAsync(userId, ct);
        }

        return Unit.Value;
    }
}
