using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Admin.Backups;
using LibraryConnect.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>Phân hệ I.5 — Sao lưu và phục hồi cơ sở dữ liệu.</summary>
[Route("api/admin/backups")]
[Tags("Quản trị hệ thống — Sao lưu")]
public class BackupsController : ApiControllerBase
{
    /// <summary>Danh sách các bản sao lưu đã thực hiện, mới nhất trước.</summary>
    [HttpGet]
    [RequirePermission(PermissionCodes.SystemBackupView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<BackupJobDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<BackupJobDto>>>> GetList(
        [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetBackupsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Dung lượng còn trống và cấu hình sao lưu tự động đang áp dụng.</summary>
    [HttpGet("storage")]
    [RequirePermission(PermissionCodes.SystemBackupView)]
    [ProducesResponseType(typeof(ApiResponse<BackupStorageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BackupStorageDto>>> GetStorage(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetBackupStorageQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>
    /// Sao lưu ngay bằng pg_dump. Với cơ sở dữ liệu lớn, thao tác có thể mất vài phút.
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCodes.SystemBackupCreate)]
    [ProducesResponseType(typeof(ApiResponse<BackupJobDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BackupJobDto>>> Create(
        [FromBody] CreateBackupRequest body, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new CreateBackupCommand(body.Type, body.IncludeObjectStorage), ct);

        return result.Status == BackupStatus.Success
            ? Ok(Success(result, "Sao lưu thành công."))
            : Ok(ApiResponse<BackupJobDto>.Fail(result.Message ?? "Sao lưu thất bại."));
    }

    /// <summary>Tải bản sao lưu về máy. Thao tác được ghi vào nhật ký hệ thống.</summary>
    [HttpGet("{id:guid}/download")]
    [RequirePermission(PermissionCodes.SystemBackupView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var (content, fileName) = await Mediator.Send(new DownloadBackupQuery(id), ct);
        return File(content, "application/octet-stream", fileName);
    }

    /// <summary>
    /// Phục hồi cơ sở dữ liệu từ một bản sao lưu.
    ///
    /// **Thao tác này ghi đè toàn bộ dữ liệu hiện tại.** Người dùng phải nhập lại mật khẩu của chính
    /// mình để xác nhận; giao diện hiển thị cảnh báo hai bước trước khi gọi endpoint này.
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    [RequirePermission(PermissionCodes.SystemBackupRestore)]
    [ProducesResponseType(typeof(ApiResponse<BackupJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<BackupJobDto>>> Restore(
        Guid id, [FromBody] RestoreBackupRequest body, CancellationToken ct)
    {
        var result = await Mediator.Send(new RestoreBackupCommand(id, body.ConfirmPassword), ct);
        return Ok(Success(result, "Phục hồi cơ sở dữ liệu thành công. Vui lòng đăng nhập lại."));
    }

    /// <summary>Xóa một bản sao lưu khỏi máy chủ.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionCodes.SystemBackupDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteBackupCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa bản sao lưu."));
    }
}

public class CreateBackupRequest
{
    public BackupType Type { get; set; } = BackupType.Full;
    /// <summary>Sao lưu kèm tệp tài liệu số trong MinIO.</summary>
    public bool IncludeObjectStorage { get; set; } = true;
}

public class RestoreBackupRequest
{
    /// <summary>Mật khẩu của chính người đang đăng nhập, dùng để xác nhận thao tác phá hủy.</summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}
