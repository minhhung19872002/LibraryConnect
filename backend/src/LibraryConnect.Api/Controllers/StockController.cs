using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Acquisition;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Ấn phẩm trong kho — Phân hệ III mục III.2 và III.5: xếp giá, kiểm nhận, khóa / mở khóa, chuyển
/// kho, thanh lý, in tem mã vạch và nhãn gáy.
/// </summary>
[Route("api/stock")]
[Tags("Kho ấn phẩm")]
public class StockController : ApiControllerBase
{
    /// <summary>Danh sách ĐKCB trong kho theo bộ lọc.</summary>
    [HttpPost("items/search")]
    [RequirePermission(PermissionCodes.AcqItemView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<StockItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<StockItemDto>>>> Search(
        [FromBody] StockItemRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchStockItemsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Số ĐKCB theo từng trạng thái, cho các thẻ đếm trên đầu màn hình.</summary>
    [HttpPost("items/summary")]
    [RequirePermission(PermissionCodes.AcqItemView)]
    [ProducesResponseType(typeof(ApiResponse<StockSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StockSummaryDto>>> Summary(
        [FromBody] StockItemFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetStockSummaryQuery(filter), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một ĐKCB kèm lịch sử chuyển kho và quyết định thanh lý nếu có.</summary>
    [HttpGet("items/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqItemView)]
    [ProducesResponseType(typeof(ApiResponse<StockItemDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<StockItemDetailDto>>> Get(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetStockItemQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Xếp ĐKCB vào kho / giá và sinh ký hiệu xếp giá (III.2).</summary>
    [HttpPost("items/shelve")]
    [RequirePermission(PermissionCodes.AcqItemUpdate)]
    [ProducesResponseType(typeof(ApiResponse<BulkItemResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BulkItemResultDto>>> Shelve(
        [FromBody] AssignShelfCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, $"Đã xếp giá {result.Affected} bản."));
    }

    /// <summary>Kiểm nhận ấn phẩm và mở khóa cho lưu thông (III.5).</summary>
    [HttpPost("items/inspect")]
    [RequirePermission(PermissionCodes.AcqItemInspect)]
    [ProducesResponseType(typeof(ApiResponse<BulkItemResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BulkItemResultDto>>> Inspect(
        [FromBody] InspectItemsCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, $"Đã kiểm nhận {result.Affected} bản."));
    }

    /// <summary>Khóa hoặc mở khóa lưu thông cho một lô ĐKCB.</summary>
    [HttpPost("items/lock")]
    [RequirePermission(PermissionCodes.AcqItemLock)]
    [ProducesResponseType(typeof(ApiResponse<BulkItemResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BulkItemResultDto>>> Lock(
        [FromBody] SetItemLockCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        var verb = command.IsLocked ? "khóa" : "mở khóa";
        return Ok(Success(result, $"Đã {verb} {result.Affected} bản."));
    }

    /// <summary>Chuyển ĐKCB sang kho / giá khác và lập phiếu chuyển kho (III.5).</summary>
    [HttpPost("items/transfer")]
    [RequirePermission(PermissionCodes.AcqItemMove)]
    [ProducesResponseType(typeof(ApiResponse<BulkItemResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<BulkItemResultDto>>> Transfer(
        [FromBody] TransferItemsCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, $"Đã chuyển {result.Affected} bản, phiếu số {result.DocumentCode}."));
    }

    /// <summary>Thanh lý, ghi mất hoặc ghi hỏng không phục hồi cho một lô ĐKCB (III.5).</summary>
    [HttpPost("items/dispose")]
    [RequirePermission(PermissionCodes.AcqItemDispose)]
    [ProducesResponseType(typeof(ApiResponse<BulkItemResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<BulkItemResultDto>>> Dispose(
        [FromBody] DisposeItemsCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, $"Đã xử lý {result.Affected} bản theo quyết định {result.DocumentCode}."));
    }

    /// <summary>Xuất danh sách ĐKCB đang xem ra Excel.</summary>
    [HttpPost("items/export")]
    [RequirePermission(PermissionCodes.AcqItemExport)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportItems(
        [FromBody] ExportStockItemsBody body, CancellationToken ct)
    {
        var file = await Mediator.Send(
            new ExportStockItemsQuery(body.Filter ?? new StockItemFilter(), body.Ids), ct);

        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Danh sách phiếu chuyển kho đã lập.</summary>
    [HttpGet("transfers")]
    [RequirePermission(PermissionCodes.AcqItemMove)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TransferSlipDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TransferSlipDto>>>> Transfers(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] Guid? warehouseId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetTransferSlipsQuery(from, to, warehouseId), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một phiếu chuyển kho.</summary>
    [HttpGet("transfers/{batchCode}")]
    [RequirePermission(PermissionCodes.AcqItemMove)]
    [ProducesResponseType(typeof(ApiResponse<TransferSlipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TransferSlipDto>>> TransferSlip(
        string batchCode, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetTransferSlipQuery(batchCode), ct);
        return Ok(Success(result));
    }

    // ---------------------------------------------------------------
    // Mẫu tem mã vạch và nhãn gáy (III.2)
    // ---------------------------------------------------------------

    /// <summary>Danh sách mẫu tem mã vạch.</summary>
    [HttpGet("barcode-templates")]
    [RequirePermission(PermissionCodes.AcqItemPrintBarcode)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BarcodeTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BarcodeTemplateDto>>>> BarcodeTemplates(
        [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetBarcodeTemplatesQuery(includeInactive), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm mẫu tem mã vạch.</summary>
    [HttpPost("barcode-templates")]
    [RequirePermission(PermissionCodes.AcqItemPrintBarcode)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateBarcodeTemplate(
        [FromBody] SaveBarcodeTemplateCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã thêm mẫu tem."));
    }

    /// <summary>Sửa mẫu tem mã vạch.</summary>
    [HttpPut("barcode-templates/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqItemPrintBarcode)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateBarcodeTemplate(
        Guid id, [FromBody] SaveBarcodeTemplateCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu mẫu tem."));
    }

    /// <summary>Xóa mẫu tem mã vạch.</summary>
    [HttpDelete("barcode-templates/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqItemPrintBarcode)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteBarcodeTemplate(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteBarcodeTemplateCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa mẫu tem."));
    }

    /// <summary>Danh sách mẫu nhãn gáy.</summary>
    [HttpGet("label-templates")]
    [RequirePermission(PermissionCodes.AcqItemPrintLabel)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LabelTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LabelTemplateDto>>>> LabelTemplates(
        [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetLabelTemplatesQuery(includeInactive), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm mẫu nhãn gáy.</summary>
    [HttpPost("label-templates")]
    [RequirePermission(PermissionCodes.AcqItemPrintLabel)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateLabelTemplate(
        [FromBody] SaveLabelTemplateCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã thêm mẫu nhãn."));
    }

    /// <summary>Sửa mẫu nhãn gáy.</summary>
    [HttpPut("label-templates/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqItemPrintLabel)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateLabelTemplate(
        Guid id, [FromBody] SaveLabelTemplateCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu mẫu nhãn."));
    }

    /// <summary>Xóa mẫu nhãn gáy.</summary>
    [HttpDelete("label-templates/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqItemPrintLabel)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteLabelTemplate(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteLabelTemplateCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa mẫu nhãn."));
    }

    /// <summary>In tem mã vạch ra PDF theo khổ tờ tem của mẫu.</summary>
    [HttpPost("print/barcodes")]
    [RequirePermission(PermissionCodes.AcqItemPrintBarcode)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PrintBarcodes(
        [FromBody] PrintBarcodesCommand command, CancellationToken ct)
    {
        var file = await Mediator.Send(command, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>In nhãn gáy sách ra PDF.</summary>
    [HttpPost("print/labels")]
    [RequirePermission(PermissionCodes.AcqItemPrintLabel)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PrintLabels(
        [FromBody] PrintSpineLabelsCommand command, CancellationToken ct)
    {
        var file = await Mediator.Send(command, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>
    /// Ảnh PNG của một mã vạch, dùng cho ô xem trước trên màn hình thiết kế mẫu tem.
    /// </summary>
    [HttpGet("barcode-image")]
    [RequirePermission(PermissionCodes.AcqItemPrintBarcode)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult BarcodeImage(
        [FromQuery] string value,
        [FromQuery] Domain.Enums.BarcodeType type = Domain.Enums.BarcodeType.Code128,
        [FromQuery] int width = 400,
        [FromQuery] int height = 120)
    {
        var printer = HttpContext.RequestServices
            .GetRequiredService<Application.Common.Interfaces.ILabelPrintService>();

        var png = printer.RenderBarcodeImage(
            value, type, Math.Clamp(width, 40, 2000), Math.Clamp(height, 20, 2000));

        return File(png, "image/png");
    }
}

public class ExportStockItemsBody
{
    public StockItemFilter? Filter { get; set; }
    public List<Guid>? Ids { get; set; }
}
