using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Admin.Users;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>Phân hệ I.2 — Quản lý tài khoản cán bộ thư viện.</summary>
[Route("api/admin/users")]
[Tags("Quản trị hệ thống — Người dùng")]
public class UsersController : ApiControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Danh sách người dùng: lọc theo nhóm, trạng thái, đơn vị, tình trạng khóa.</summary>
    [HttpGet]
    [RequirePermission(PermissionCodes.SystemUserView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserListItemDto>>>> GetList(
        [FromQuery] UserListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetUsersQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một người dùng kèm nhóm quyền và phạm vi dữ liệu được gán.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCodes.SystemUserView)]
    [AuditRead("User")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetUserByIdQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Danh sách đơn vị đang có, dùng cho bộ lọc.</summary>
    [HttpGet("departments")]
    [RequirePermission(PermissionCodes.SystemUserView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<string>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<string>>>> GetDepartments(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetUserDepartmentsQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>
    /// Thêm người dùng. Bỏ trống mật khẩu để hệ thống sinh mật khẩu tạm; mật khẩu chỉ được trả về
    /// một lần duy nhất trong phản hồi này.
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCodes.SystemUserCreate)]
    [ProducesResponseType(typeof(ApiResponse<CreateUserResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CreateUserResult>>> Create(
        [FromBody] CreateUserCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Thêm người dùng thành công. Người dùng phải đổi mật khẩu ở lần đăng nhập đầu tiên."));
    }

    /// <summary>Sửa thông tin, nhóm quyền và phạm vi dữ liệu của người dùng.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionCodes.SystemUserUpdate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Update(
        Guid id, [FromBody] UserProfileInput profile, CancellationToken ct)
    {
        await Mediator.Send(new UpdateUserCommand(id, profile), ct);
        return Ok(SuccessMessage("Cập nhật người dùng thành công."));
    }

    /// <summary>Xóa người dùng (xóa mềm, dữ liệu vẫn được lưu trữ vĩnh viễn).</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionCodes.SystemUserDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteUserCommand(id), ct);
        return Ok(SuccessMessage("Xóa người dùng thành công."));
    }

    /// <summary>
    /// Đặt lại mật khẩu. Mọi phiên đăng nhập hiện tại của tài khoản bị thu hồi và người dùng buộc
    /// phải đổi mật khẩu ở lần đăng nhập kế tiếp.
    /// </summary>
    [HttpPost("{id:guid}/reset-password")]
    [RequirePermission(PermissionCodes.SystemUserResetPassword)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword(
        Guid id, [FromBody] ResetPasswordRequest? body, CancellationToken ct)
    {
        var password = await Mediator.Send(new ResetUserPasswordCommand(id, body?.NewPassword), ct);
        return Ok(Success(password, "Đặt lại mật khẩu thành công."));
    }

    /// <summary>Khóa hoặc mở khóa tài khoản.</summary>
    [HttpPost("{id:guid}/lock")]
    [RequirePermission(PermissionCodes.SystemUserLock)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> SetLock(
        Guid id, [FromBody] LockUserRequest body, CancellationToken ct)
    {
        await Mediator.Send(new SetUserLockCommand(id, body.Locked, body.Reason), ct);
        return Ok(SuccessMessage(body.Locked ? "Đã khóa tài khoản." : "Đã mở khóa tài khoản."));
    }

    /// <summary>Lịch sử đăng nhập của một tài khoản, gồm cả các lần đăng nhập thất bại.</summary>
    [HttpGet("{id:guid}/login-history")]
    [RequirePermission(PermissionCodes.SystemUserView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<LoginHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<LoginHistoryDto>>>> GetLoginHistory(
        Guid id, [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetUserLoginHistoryQuery(id, request), ct);
        return Ok(Success(result));
    }

    /// <summary>Tải tệp Excel mẫu để nhập người dùng hàng loạt.</summary>
    [HttpGet("import-template")]
    [RequirePermission(PermissionCodes.SystemUserImport)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImportTemplate(CancellationToken ct)
    {
        var content = await Mediator.Send(new GetUserImportTemplateQuery(), ct);
        return File(content, ExcelContentType, "Mau-nhap-nguoi-dung.xlsx");
    }

    /// <summary>
    /// Nhập người dùng từ Excel. Đặt <c>dryRun=true</c> để kiểm tra tệp trước: hệ thống trả về đúng
    /// danh sách lỗi mà không ghi bất kỳ bản ghi nào.
    /// </summary>
    [HttpPost("import")]
    [RequirePermission(PermissionCodes.SystemUserImport)]
    [ProducesResponseType(typeof(ApiResponse<UserImportResultDto>), StatusCodes.Status200OK)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<UserImportResultDto>>> Import(
        IFormFile file, [FromQuery] bool dryRun, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Vui lòng chọn tệp Excel cần nhập."));
        }

        await using var stream = file.OpenReadStream();
        var result = await Mediator.Send(new ImportUsersCommand(stream, file.FileName, dryRun), ct);

        var message = dryRun
            ? $"Kiểm tra xong: {result.SuccessRows}/{result.TotalRows} dòng hợp lệ."
            : $"Nhập thành công {result.SuccessRows}/{result.TotalRows} dòng.";

        return Ok(Success(result, message));
    }
}

public class ResetPasswordRequest
{
    /// <summary>Bỏ trống để hệ thống sinh mật khẩu tạm theo chính sách đang cấu hình.</summary>
    public string? NewPassword { get; set; }
}

public class LockUserRequest
{
    public bool Locked { get; set; }
    public string? Reason { get; set; }
}
