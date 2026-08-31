using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Admin.AuditLogs;
using LibraryConnect.Application.Features.Admin.Parameters;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>Phân hệ I.3 — Tham số hệ thống.</summary>
[Route("api/admin/parameters")]
[Tags("Quản trị hệ thống — Tham số")]
public class ParametersController : ApiControllerBase
{
    /// <summary>Toàn bộ tham số, nhóm theo chủ đề. Giá trị của tham số bí mật không được trả về.</summary>
    [HttpGet]
    [RequirePermission(PermissionCodes.SystemParameterView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ParameterGroupDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ParameterGroupDto>>>> GetAll(
        [FromQuery] string? groupCode, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetParametersQuery(groupCode), ct);
        return Ok(Success(result));
    }

    /// <summary>Lưu nhiều tham số cùng lúc. Mỗi thay đổi được ghi vào lịch sử tham số.</summary>
    [HttpPut]
    [RequirePermission(PermissionCodes.SystemParameterUpdate)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<int>>> Update(
        [FromBody] UpdateParametersRequest body, CancellationToken ct)
    {
        var changed = await Mediator.Send(new UpdateParametersCommand(body.Parameters), ct);

        return Ok(Success(changed, changed == 0
            ? "Không có tham số nào thay đổi."
            : $"Đã cập nhật {changed} tham số."));
    }

    /// <summary>Lịch sử thay đổi tham số: ai đổi, từ giá trị nào sang giá trị nào, lúc nào.</summary>
    [HttpGet("history")]
    [RequirePermission(PermissionCodes.SystemParameterView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ParameterHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParameterHistoryDto>>>> GetHistory(
        [FromQuery] string? key, [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetParameterHistoryQuery(key, request), ct);
        return Ok(Success(result));
    }
}

public class UpdateParametersRequest
{
    public List<ParameterUpdateInput> Parameters { get; set; } = new();
}

/// <summary>Phân hệ I.4 — Nhật ký hệ thống.</summary>
[Route("api/admin/audit-logs")]
[Tags("Quản trị hệ thống — Nhật ký")]
public class AuditLogsController : ApiControllerBase
{
    /// <summary>Tra cứu nhật ký theo thời gian, người dùng, hành động, đối tượng, kết quả và IP.</summary>
    [HttpGet]
    [RequirePermission(PermissionCodes.SystemAuditView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogListItemDto>>>> GetList(
        [FromQuery] AuditLogListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetAuditLogsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một bản ghi nhật ký kèm giá trị cũ và mới dạng JSON để so sánh.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCodes.SystemAuditView)]
    [ProducesResponseType(typeof(ApiResponse<AuditLogDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuditLogDetailDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetAuditLogByIdQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Các giá trị hiện có trong nhật ký để dựng dropdown bộ lọc.</summary>
    [HttpGet("filter-options")]
    [RequirePermission(PermissionCodes.SystemAuditView)]
    [ProducesResponseType(typeof(ApiResponse<AuditFilterOptionsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuditFilterOptionsDto>>> GetFilterOptions(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetAuditFilterOptionsQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Xuất nhật ký theo đúng bộ lọc đang áp dụng ra Excel hoặc PDF.</summary>
    [HttpGet("export")]
    [RequirePermission(PermissionCodes.SystemAuditExport)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery] AuditLogListRequest request, [FromQuery] ExportFormat format, CancellationToken ct)
    {
        var file = await Mediator.Send(new ExportAuditLogsQuery(request, format), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Cài đặt chế độ ghi nhận nhật ký cho từng đối tượng nghiệp vụ.</summary>
    [HttpGet("settings")]
    [RequirePermission(PermissionCodes.SystemAuditSetting)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AuditSettingDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AuditSettingDto>>>> GetSettings(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetAuditSettingsQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Lưu cài đặt ghi nhận. Bỏ trống thời gian lưu trữ nghĩa là giữ vĩnh viễn.</summary>
    [HttpPut("settings")]
    [RequirePermission(PermissionCodes.SystemAuditSetting)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> UpdateSettings(
        [FromBody] UpdateAuditSettingsRequest body, CancellationToken ct)
    {
        var changed = await Mediator.Send(new UpdateAuditSettingsCommand(body.Settings), ct);

        return Ok(Success(changed, changed == 0
            ? "Không có thay đổi nào."
            : $"Đã cập nhật cài đặt ghi nhật ký cho {changed} đối tượng."));
    }
}

public class UpdateAuditSettingsRequest
{
    public List<AuditSettingDto> Settings { get; set; } = new();
}
