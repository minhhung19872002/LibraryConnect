using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>Phân hệ III mục III.4 — quản lý kiểm kê.</summary>
[Route("api/inventory")]
[Tags("Kiểm kê")]
public class InventoryController : ApiControllerBase
{
    /// <summary>Đóng hoặc mở kho. Kho đóng thì ngưng lưu thông và chuyển kho tại kho đó.</summary>
    [HttpPost("warehouses/{warehouseId:guid}/closed")]
    [RequirePermission(PermissionCodes.AcqInventoryCreate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> SetClosed(
        Guid warehouseId, [FromBody] SetWarehouseClosedBody body, CancellationToken ct)
    {
        await Mediator.Send(new SetWarehouseClosedCommand(warehouseId, body.Closed), ct);
        return Ok(SuccessMessage(body.Closed ? "Đã đóng kho để kiểm kê." : "Đã mở lại kho."));
    }

    /// <summary>Danh sách kỳ kiểm kê.</summary>
    [HttpGet("periods")]
    [RequirePermission(PermissionCodes.AcqInventoryView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<InventoryPeriodDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<InventoryPeriodDto>>>> GetPeriods(
        [FromQuery] InventoryPeriodListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchInventoryPeriodsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một kỳ kiểm kê.</summary>
    [HttpGet("periods/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqInventoryView)]
    [ProducesResponseType(typeof(ApiResponse<InventoryPeriodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InventoryPeriodDto>>> GetPeriod(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetInventoryPeriodQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Tạo kỳ kiểm kê và chốt danh sách ĐKCB kỳ vọng.</summary>
    [HttpPost("periods")]
    [RequirePermission(PermissionCodes.AcqInventoryCreate)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreatePeriod(
        [FromBody] CreateInventoryPeriodCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã tạo kỳ kiểm kê và chốt danh sách ấn phẩm kỳ vọng."));
    }

    /// <summary>Phân công lại cán bộ cho kỳ kiểm kê đang chạy.</summary>
    [HttpPut("periods/{id:guid}/staff")]
    [RequirePermission(PermissionCodes.AcqInventoryCreate)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<int>>> AssignStaff(
        Guid id, [FromBody] AssignInventoryStaffCommand command, CancellationToken ct)
    {
        command.PeriodId = id;
        var count = await Mediator.Send(command, ct);
        return Ok(Success(count, $"Đã phân công {count} cán bộ cho kỳ kiểm kê."));
    }

    /// <summary>Ghi nhận một lần quét mã vạch, phản hồi ngay khớp / thừa / sai kho.</summary>
    [HttpPost("periods/{id:guid}/scan")]
    [RequirePermission(PermissionCodes.AcqInventoryScan)]
    [ProducesResponseType(typeof(ApiResponse<InventoryScanResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<InventoryScanResultDto>>> Scan(
        Guid id, [FromBody] ScanInventoryCommand command, CancellationToken ct)
    {
        command.PeriodId = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, result.Message));
    }

    /// <summary>Nạp tệp quét từ máy đọc rời: mỗi dòng một mã vạch.</summary>
    [HttpPost("periods/{id:guid}/scan-file")]
    [RequirePermission(PermissionCodes.AcqInventoryScan)]
    [ProducesResponseType(typeof(ApiResponse<ImportInventoryScansResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ImportInventoryScansResultDto>>> ImportScans(
        Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Chưa chọn tệp quét."));
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync(ct);

        var result = await Mediator.Send(new ImportInventoryScansCommand
        {
            PeriodId = id,
            Content = content
        }, ct);

        return Ok(Success(result, $"Đã nạp {result.Total} mã vạch từ tệp quét."));
    }

    /// <summary>Số liệu tiến độ và kết quả của một kỳ kiểm kê.</summary>
    [HttpGet("periods/{id:guid}/summary")]
    [RequirePermission(PermissionCodes.AcqInventoryView)]
    [ProducesResponseType(typeof(ApiResponse<InventorySummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InventorySummaryDto>>> Summary(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetInventorySummaryQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Danh sách kết quả kiểm kê: khớp, thiếu, thừa, sai kho.</summary>
    [HttpGet("periods/{id:guid}/results")]
    [RequirePermission(PermissionCodes.AcqInventoryReport)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<InventoryResultRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<InventoryResultRowDto>>>> Results(
        Guid id, [FromQuery] InventoryResultRequest request, CancellationToken ct)
    {
        request.PeriodId = id;
        var result = await Mediator.Send(new GetInventoryResultsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Xuất kết quả kiểm kê ra Excel.</summary>
    [HttpGet("periods/{id:guid}/results/export")]
    [RequirePermission(PermissionCodes.AcqInventoryReport)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportResults(
        Guid id, [FromQuery] InventoryResultType? result, CancellationToken ct)
    {
        var file = await Mediator.Send(new ExportInventoryResultsQuery(id, result), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Chốt kỳ kiểm kê, đối chiếu và mở lại kho.</summary>
    [HttpPost("periods/{id:guid}/close")]
    [RequirePermission(PermissionCodes.AcqInventoryClose)]
    [ProducesResponseType(typeof(ApiResponse<InventorySummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<InventorySummaryDto>>> Close(
        Guid id, [FromBody] CloseInventoryPeriodCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result,
            $"Đã chốt kỳ kiểm kê: khớp {result.MatchCount}, thiếu {result.MissingCount}, " +
            $"thừa {result.UnexpectedCount}, sai kho {result.WrongWarehouseCount}."));
    }

    /// <summary>Từ danh sách thiếu, lập quyết định thanh lý hoặc ghi mất.</summary>
    [HttpPost("periods/{id:guid}/resolve-missing")]
    [RequirePermission(PermissionCodes.AcqItemDispose)]
    [ProducesResponseType(typeof(ApiResponse<BulkItemResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<BulkItemResultDto>>> ResolveMissing(
        Guid id, [FromBody] ResolveMissingItemsCommand command, CancellationToken ct)
    {
        command.PeriodId = id;
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result,
            $"Đã xử lý {result.Affected} bản thiếu theo quyết định {result.DocumentCode}."));
    }
}

public class SetWarehouseClosedBody
{
    public bool Closed { get; set; }
}
