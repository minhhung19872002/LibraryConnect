using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Admin.AuditLogs;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Phân hệ III mục III.2 và III.7 — báo cáo bổ sung, và mục III.6 — mẫu biểu in.
/// </summary>
[Route("api/acquisition/reports")]
[Tags("Bổ sung — Báo cáo")]
public class AcquisitionReportsController : ApiControllerBase
{
    /// <summary>Danh sách tài liệu bổ sung theo bộ lọc.</summary>
    [HttpPost("acquisition-list")]
    [RequirePermission(PermissionCodes.AcqReportView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AcquisitionListRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AcquisitionListRowDto>>>> AcquisitionList(
        [FromBody] AcquisitionReportFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetAcquisitionListReportQuery(filter), ct);
        return Ok(Success(result));
    }

    /// <summary>Danh sách ĐKCB đã thanh lý, ghi mất hoặc hỏng không phục hồi.</summary>
    [HttpPost("disposals")]
    [RequirePermission(PermissionCodes.AcqReportView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DisposalReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DisposalReportRowDto>>>> Disposals(
        [FromBody] AcquisitionReportFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetDisposalReportQuery(filter), ct);
        return Ok(Success(result));
    }

    /// <summary>Báo cáo tổng quát: tổng số biểu ghi, ĐKCB và phân bổ theo kho, dạng tài liệu, tình trạng.</summary>
    [HttpPost("overview")]
    [RequirePermission(PermissionCodes.AcqReportView)]
    [ProducesResponseType(typeof(ApiResponse<StockOverviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StockOverviewDto>>> Overview(
        [FromBody] AcquisitionReportFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetStockOverviewQuery(filter), ct);
        return Ok(Success(result));
    }

    /// <summary>
    /// Thống kê bổ sung theo một chiều: dạng tài liệu, vật mang tin, thời gian, ngôn ngữ, kho,
    /// nguồn kinh phí, hình thức bổ sung, tình trạng hoặc nhà cung cấp (III.7).
    /// </summary>
    [HttpPost("statistics")]
    [RequirePermission(PermissionCodes.AcqReportView)]
    [ProducesResponseType(typeof(ApiResponse<AcquisitionStatReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AcquisitionStatReportDto>>> Statistics(
        [FromQuery] string dimension,
        [FromQuery] TimeGrouping grouping,
        [FromBody] AcquisitionReportFilter filter,
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetAcquisitionStatsQuery(dimension, filter, grouping), ct);
        return Ok(Success(result));
    }

    /// <summary>Bảng tổng hợp đa chiều: tự chọn chiều hàng, chiều cột và chỉ tiêu.</summary>
    [HttpPost("pivot")]
    [RequirePermission(PermissionCodes.AcqReportView)]
    [ProducesResponseType(typeof(ApiResponse<AcquisitionPivotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AcquisitionPivotDto>>> Pivot(
        [FromQuery] string rowDimension,
        [FromQuery] string columnDimension,
        [FromQuery] PivotMeasure measure,
        [FromQuery] TimeGrouping grouping,
        [FromBody] AcquisitionReportFilter filter,
        CancellationToken ct)
    {
        var result = await Mediator.Send(
            new GetAcquisitionPivotQuery(rowDimension, columnDimension, measure, filter, grouping), ct);

        return Ok(Success(result));
    }

    /// <summary>Các chiều thống kê dùng được, để dựng ô chọn trên màn hình báo cáo.</summary>
    [HttpGet("dimensions")]
    [RequirePermission(PermissionCodes.AcqReportView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyDictionary<string, string>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyDictionary<string, string>>> Dimensions() =>
        Ok(Success(AcquisitionDimensions.Labels));

    /// <summary>Báo cáo duyệt mua: số yêu cầu theo trạng thái, đơn vị, thời gian và tỷ lệ duyệt.</summary>
    [HttpGet("purchase-approval")]
    [RequirePermission(PermissionCodes.AcqReportView)]
    [ProducesResponseType(typeof(ApiResponse<PurchaseApprovalReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PurchaseApprovalReportDto>>> PurchaseApproval(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPurchaseApprovalReportQuery(from, to), ct);
        return Ok(Success(result));
    }

    /// <summary>Lịch sử giao dịch với một nhà cung cấp.</summary>
    [HttpGet("suppliers/{id:guid}")]
    [RequirePermission(PermissionCodes.AcqReportView)]
    [ProducesResponseType(typeof(ApiResponse<SupplierHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SupplierHistoryDto>>> SupplierHistory(
        Guid id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetSupplierHistoryQuery(id, from, to), ct);
        return Ok(Success(result));
    }

    /// <summary>Xuất báo cáo bổ sung ra Excel hoặc PDF.</summary>
    [HttpPost("export")]
    [RequirePermission(PermissionCodes.AcqReportExport)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Export(
        [FromQuery] AcquisitionReportKind kind,
        [FromQuery] ExportFormat format,
        [FromQuery] string? dimension,
        [FromQuery] string? columnDimension,
        [FromQuery] PivotMeasure measure,
        [FromQuery] TimeGrouping grouping,
        [FromBody] AcquisitionReportFilter filter,
        CancellationToken ct)
    {
        var file = await Mediator.Send(new ExportAcquisitionReportQuery(
            kind, filter, format, dimension, columnDimension, measure, grouping), ct);

        return File(file.Content, file.ContentType, file.FileName);
    }
}

/// <summary>Phân hệ III mục III.6 — trình thiết kế biểu mẫu in dùng chung.</summary>
[Route("api/acquisition/forms")]
[Tags("Bổ sung — Mẫu biểu in")]
public class FormTemplatesController : ApiControllerBase
{
    /// <summary>Các loại biểu mẫu và trường dữ liệu dùng được của từng loại.</summary>
    [HttpGet("types")]
    [RequirePermission(PermissionCodes.AcqFormTemplateManage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FormTypeMetadataDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FormTypeMetadataDto>>>> Types(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetFormTypesQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Danh sách mẫu biểu in.</summary>
    [HttpGet]
    [RequirePermission(PermissionCodes.AcqFormTemplateManage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FormTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FormTemplateDto>>>> GetAll(
        [FromQuery] string? formType, [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetFormTemplatesQuery(formType, includeInactive), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm mẫu biểu in.</summary>
    [HttpPost]
    [RequirePermission(PermissionCodes.AcqFormTemplateManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] SaveFormTemplateCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã thêm mẫu biểu."));
    }

    /// <summary>Sửa mẫu biểu in.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionCodes.AcqFormTemplateManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> Update(
        Guid id, [FromBody] SaveFormTemplateCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu mẫu biểu."));
    }

    /// <summary>Xóa mẫu biểu in.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionCodes.AcqFormTemplateManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteFormTemplateCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa mẫu biểu."));
    }

    /// <summary>
    /// In một chứng từ theo mẫu. <c>documentId</c> là mã chứng từ: mã đơn đặt, mã biên bản, số phiếu
    /// chuyển kho, số quyết định thanh lý hoặc mã kỳ kiểm kê.
    /// </summary>
    [HttpGet("print/{formType}/{documentId}")]
    // Cổng thô: có quyền in một loại mẫu nào đó. Quyền đúng theo loại mẫu kiểm trong handler
    // (FormTypes.PermissionsToPrint) — phiếu mượn không đòi quyền của phân hệ Bổ sung.
    [RequirePermission(false,
        PermissionCodes.AcqOrderPrint,
        PermissionCodes.CirculationLoanReturn, PermissionCodes.CirculationLoanView,
        PermissionCodes.CirculationFineCollect, PermissionCodes.CirculationFineView,
        PermissionCodes.ReaderView,
        PermissionCodes.AcqItemMove, PermissionCodes.AcqItemView, PermissionCodes.AcqItemDispose,
        PermissionCodes.AcqInventoryReport, PermissionCodes.AcqInventoryView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Print(
        string formType, string documentId, [FromQuery] Guid? templateId, CancellationToken ct)
    {
        var file = await Mediator.Send(new PrintFormCommand(formType, documentId, templateId), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
