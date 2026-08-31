using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Admin.UserGroups;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>Phân hệ I.1 — Quản lý nhóm người dùng và phân quyền theo nhóm.</summary>
[Route("api/admin/user-groups")]
[Tags("Quản trị hệ thống — Nhóm người dùng")]
public class UserGroupsController : ApiControllerBase
{
    /// <summary>Danh sách nhóm, có tìm kiếm và lọc theo trạng thái.</summary>
    [HttpGet]
    [RequirePermission(PermissionCodes.SystemGroupView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserGroupListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserGroupListItemDto>>>> GetList(
        [FromQuery] UserGroupListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetUserGroupsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một nhóm kèm danh sách mã quyền đang được cấp.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCodes.SystemGroupView)]
    [ProducesResponseType(typeof(ApiResponse<UserGroupDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserGroupDetailDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetUserGroupByIdQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm nhóm mới.</summary>
    [HttpPost]
    [RequirePermission(PermissionCodes.SystemGroupCreate)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateUserGroupCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Thêm nhóm người dùng thành công."));
    }

    /// <summary>Sửa thông tin nhóm. Mã nhóm không thay đổi được sau khi tạo.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionCodes.SystemGroupUpdate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Update(Guid id, [FromBody] UpdateUserGroupRequest body, CancellationToken ct)
    {
        await Mediator.Send(new UpdateUserGroupCommand(id, body.Name, body.Description, body.IsActive), ct);
        return Ok(SuccessMessage("Cập nhật nhóm người dùng thành công."));
    }

    /// <summary>Xóa nhóm. Nhóm hệ thống hoặc nhóm còn thành viên sẽ bị từ chối.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionCodes.SystemGroupDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, [FromQuery] string? reason, CancellationToken ct)
    {
        await Mediator.Send(new DeleteUserGroupCommand(id, reason), ct);
        return Ok(SuccessMessage("Xóa nhóm người dùng thành công."));
    }

    /// <summary>Cây quyền phân cấp Module → Chức năng → Hành động, kèm các quyền đang được cấp.</summary>
    [HttpGet("{id:guid}/permissions")]
    [RequirePermission(PermissionCodes.SystemGroupView)]
    [ProducesResponseType(typeof(ApiResponse<GroupPermissionsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GroupPermissionsDto>>> GetPermissions(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetGroupPermissionsQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Đặt lại toàn bộ danh sách quyền của nhóm.</summary>
    [HttpPut("{id:guid}/permissions")]
    [RequirePermission(PermissionCodes.SystemGroupPermission)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> UpdatePermissions(
        Guid id, [FromBody] UpdatePermissionsRequest body, CancellationToken ct)
    {
        await Mediator.Send(new UpdateGroupPermissionsCommand(id, body.PermissionCodes), ct);
        return Ok(SuccessMessage("Cập nhật quyền cho nhóm thành công."));
    }

    /// <summary>Sao chép bộ quyền từ một nhóm khác (thay thế hoặc gộp thêm).</summary>
    [HttpPost("{id:guid}/clone")]
    [RequirePermission(PermissionCodes.SystemGroupPermission)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> ClonePermissions(
        Guid id, [FromBody] CloneGroupRequest body, CancellationToken ct)
    {
        var count = await Mediator.Send(new CloneGroupPermissionsCommand(id, body.SourceGroupId, body.Replace), ct);
        return Ok(Success(count, $"Đã sao chép {count} quyền sang nhóm."));
    }

    /// <summary>Danh sách thành viên của nhóm.</summary>
    [HttpGet("{id:guid}/members")]
    [RequirePermission(PermissionCodes.SystemGroupView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<GroupMemberDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<GroupMemberDto>>>> GetMembers(
        Guid id, [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetGroupMembersQuery(id, request), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm và bớt thành viên hàng loạt trong một lần gọi.</summary>
    [HttpPut("{id:guid}/members")]
    [RequirePermission(PermissionCodes.SystemGroupUpdate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> UpdateMembers(
        Guid id, [FromBody] UpdateMembersRequest body, CancellationToken ct)
    {
        await Mediator.Send(new UpdateGroupMembersCommand(id, body.AddUserIds, body.RemoveUserIds), ct);
        return Ok(SuccessMessage("Cập nhật thành viên nhóm thành công."));
    }
}

public class UpdateUserGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdatePermissionsRequest
{
    /// <summary>Danh sách mã quyền được cấp. Khóa nhánh của cây (module:/group:) sẽ được bỏ qua.</summary>
    public List<string> PermissionCodes { get; set; } = new();
}

public class CloneGroupRequest
{
    public Guid SourceGroupId { get; set; }
    /// <summary>True: thay thế toàn bộ quyền hiện có. False: gộp thêm vào quyền đang có.</summary>
    public bool Replace { get; set; } = true;
}

public class UpdateMembersRequest
{
    public List<Guid> AddUserIds { get; set; } = new();
    public List<Guid> RemoveUserIds { get; set; } = new();
}
