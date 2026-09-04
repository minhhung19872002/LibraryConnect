using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Digital;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Phân hệ V — Tài liệu số: kho tài liệu, trình đọc trực tuyến, yêu cầu đọc tài liệu hạn chế,
/// nhập xuất và báo cáo.
/// </summary>
[Route("api/digital")]
[Tags("Tài liệu số")]
public class DigitalController : ApiControllerBase
{
    // ---------------------------------------------------------------
    // Bộ sưu tập
    // ---------------------------------------------------------------

    /// <summary>Cây bộ sưu tập kèm số tài liệu từng nhánh.</summary>
    [HttpGet("collections")]
    [RequirePermission(PermissionCodes.DigitalView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DigitalCollectionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DigitalCollectionDto>>>> Collections(
        [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetDigitalCollectionsQuery(includeInactive), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm một bộ sưu tập.</summary>
    [HttpPost("collections")]
    [RequirePermission(PermissionCodes.DigitalCollectionManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateCollection(
        [FromBody] SaveDigitalCollectionCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã thêm bộ sưu tập."));
    }

    /// <summary>Sửa một bộ sưu tập.</summary>
    [HttpPut("collections/{id:guid}")]
    [RequirePermission(PermissionCodes.DigitalCollectionManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateCollection(
        Guid id, [FromBody] SaveDigitalCollectionCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu bộ sưu tập."));
    }

    /// <summary>Xóa một bộ sưu tập rỗng.</summary>
    [HttpDelete("collections/{id:guid}")]
    [RequirePermission(PermissionCodes.DigitalCollectionManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteCollection(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteDigitalCollectionCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa bộ sưu tập."));
    }

    // ---------------------------------------------------------------
    // Tài liệu
    // ---------------------------------------------------------------

    /// <summary>Danh sách tài liệu số, có tìm kiếm toàn văn trong nội dung.</summary>
    [HttpPost("documents/search")]
    [RequirePermission(PermissionCodes.DigitalView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DigitalDocumentRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<DigitalDocumentRowDto>>>> Search(
        [FromBody] DigitalDocumentQueryRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchDigitalDocumentsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một tài liệu số kèm quyền của người đang xem.</summary>
    [HttpGet("documents/{id:guid}")]
    [RequirePermission(PermissionCodes.DigitalView)]
    [AuditRead("DigitalDocument")]
    [ProducesResponseType(typeof(ApiResponse<DigitalDocumentDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DigitalDocumentDetailDto>>> Detail(
        Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetDigitalDocumentQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Tải một tệp tài liệu số lên trong một lần gọi.</summary>
    [HttpPost("documents/upload")]
    [RequirePermission(PermissionCodes.DigitalUpload)]
    [RequestSizeLimit(200L * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> Upload(
        IFormFile file,
        [FromForm] string? title,
        [FromForm] string? description,
        [FromForm] Guid? collectionId,
        [FromForm] Guid? bibId,
        [FromForm] Domain.Enums.DigitalAccessLevel? accessLevel,
        [FromForm] bool allowDownload,
        [FromForm] bool allowPrint,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Chưa chọn tệp."));
        }

        var id = await Mediator.Send(new UploadDigitalDocumentCommand
        {
            FileName = file.FileName,
            Content = await ReadAllAsync(file, ct),
            Title = title,
            Description = description,
            CollectionId = collectionId,
            BibId = bibId,
            AccessLevel = accessLevel,
            AllowDownload = allowDownload,
            AllowPrint = allowPrint,
        }, ct);

        return Ok(Success(id, "Đã tải tệp lên. Hệ thống đang xử lý trang bìa và nội dung tìm kiếm."));
    }

    /// <summary>Sửa thông tin và chính sách truy cập của một tài liệu số.</summary>
    [HttpPut("documents/{id:guid}")]
    [RequirePermission(PermissionCodes.DigitalUpdate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Update(
        Guid id, [FromBody] UpdateDigitalDocumentCommand command, CancellationToken ct)
    {
        command.Id = id;
        await Mediator.Send(command, ct);
        return Ok(SuccessMessage("Đã lưu tài liệu số."));
    }

    /// <summary>Xóa mềm một tài liệu số, bắt buộc ghi lý do.</summary>
    [HttpDelete("documents/{id:guid}")]
    [RequirePermission(PermissionCodes.DigitalDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Delete(
        Guid id, [FromQuery] string reason, CancellationToken ct)
    {
        await Mediator.Send(new DeleteDigitalDocumentCommand(id, reason), ct);
        return Ok(SuccessMessage("Đã xóa tài liệu số."));
    }

    /// <summary>Chạy lại nhận dạng ký tự cho một tài liệu.</summary>
    [HttpPost("documents/{id:guid}/ocr")]
    [RequirePermission(PermissionCodes.DigitalUpdate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse> RunOcr(
        Guid id, [FromServices] Application.Common.Interfaces.IBackgroundJobService jobs)
    {
        jobs.Enqueue<IDigitalProcessingJob>(job => job.RunOcrAsync(id, CancellationToken.None));
        return Ok(SuccessMessage("Đã đưa tài liệu vào hàng đợi nhận dạng ký tự."));
    }

    // ---------------------------------------------------------------
    // Tải tệp lớn theo mảnh
    // ---------------------------------------------------------------

    /// <summary>Mở một phiên tải tệp lớn.</summary>
    [HttpPost("uploads")]
    [RequirePermission(PermissionCodes.DigitalUpload)]
    [ProducesResponseType(typeof(ApiResponse<DigitalUploadSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DigitalUploadSessionDto>>> StartUpload(
        [FromBody] StartDigitalUploadCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result));
    }

    /// <summary>Trạng thái một phiên tải — dùng để biết còn thiếu mảnh nào khi tải tiếp.</summary>
    [HttpGet("uploads/{id:guid}")]
    [RequirePermission(PermissionCodes.DigitalUpload)]
    [ProducesResponseType(typeof(ApiResponse<DigitalUploadSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DigitalUploadSessionDto>>> UploadSession(
        Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetDigitalUploadSessionQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Gửi một mảnh của phiên tải.</summary>
    [HttpPost("uploads/{id:guid}/chunks/{index:int}")]
    [RequirePermission(PermissionCodes.DigitalUpload)]
    [RequestSizeLimit(64L * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<DigitalUploadSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DigitalUploadSessionDto>>> UploadChunk(
        Guid id, int index, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Mảnh tải lên rỗng."));
        }

        var result = await Mediator.Send(new UploadDigitalChunkCommand
        {
            SessionId = id,
            Index = index,
            Content = await ReadAllAsync(file, ct),
        }, ct);

        return Ok(Success(result));
    }

    /// <summary>Ghép các mảnh lại và tạo tài liệu số.</summary>
    [HttpPost("uploads/{id:guid}/complete")]
    [RequirePermission(PermissionCodes.DigitalUpload)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CompleteUpload(
        Guid id, [FromBody] CompleteDigitalUploadCommand command, CancellationToken ct)
    {
        command.SessionId = id;
        var documentId = await Mediator.Send(command, ct);
        return Ok(Success(documentId, "Đã ghép xong tệp và tạo tài liệu số."));
    }

    // ---------------------------------------------------------------
    // Đọc trực tuyến
    // ---------------------------------------------------------------

    /// <summary>Mở trình đọc: được xem tới trang nào, tài liệu dày bao nhiêu.</summary>
    [HttpGet("documents/{id:guid}/reader")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<DigitalReaderSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DigitalReaderSessionDto>>> OpenReader(
        Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new OpenDigitalReaderQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Một trang tài liệu dưới dạng ảnh PNG đã đóng chữ chìm.</summary>
    [HttpGet("documents/{id:guid}/pages/{page:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReadPage(Guid id, int page, CancellationToken ct)
    {
        var file = await Mediator.Send(new ReadDigitalPageQuery(id, page), ct);
        return File(file.Content, file.ContentType);
    }

    /// <summary>Ảnh bìa của tài liệu.</summary>
    [HttpGet("documents/{id:guid}/thumbnail")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Thumbnail(Guid id, CancellationToken ct)
    {
        var file = await Mediator.Send(new GetDigitalThumbnailQuery(id), ct);
        return File(file.Content, file.ContentType);
    }

    /// <summary>Tải bản gốc về, nếu chính sách của tài liệu cho phép.</summary>
    [HttpGet("documents/{id:guid}/download")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var file = await Mediator.Send(new DownloadDigitalDocumentQuery(id), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    // ---------------------------------------------------------------
    // Yêu cầu đọc tài liệu hạn chế
    // ---------------------------------------------------------------

    /// <summary>Danh sách yêu cầu đọc tài liệu hạn chế.</summary>
    [HttpPost("requests/search")]
    [RequirePermission(PermissionCodes.DigitalRequestView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DigitalAccessRequestRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<DigitalAccessRequestRowDto>>>> SearchRequests(
        [FromBody] DigitalRequestQueryRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchDigitalRequestsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Duyệt một yêu cầu đọc.</summary>
    [HttpPost("requests/{id:guid}/approve")]
    [RequirePermission(PermissionCodes.DigitalRequestApprove)]
    [ProducesResponseType(typeof(ApiResponse<DigitalAccessRequestRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DigitalAccessRequestRowDto>>> ApproveRequest(
        Guid id, [FromBody] ApproveDigitalRequestCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã duyệt yêu cầu đọc và thông báo cho bạn đọc."));
    }

    /// <summary>Từ chối một yêu cầu đọc kèm lý do.</summary>
    [HttpPost("requests/{id:guid}/reject")]
    [RequirePermission(PermissionCodes.DigitalRequestApprove)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> RejectRequest(
        Guid id, [FromBody] DigitalReasonRequest body, CancellationToken ct)
    {
        await Mediator.Send(new RejectDigitalRequestCommand(id, body.Reason ?? string.Empty), ct);
        return Ok(SuccessMessage("Đã từ chối yêu cầu và thông báo cho bạn đọc."));
    }

    /// <summary>Thu hồi một quyền đọc đã cấp.</summary>
    [HttpPost("requests/{id:guid}/revoke")]
    [RequirePermission(PermissionCodes.DigitalRequestApprove)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> RevokeRequest(
        Guid id, [FromBody] DigitalReasonRequest body, CancellationToken ct)
    {
        await Mediator.Send(new RevokeDigitalRequestCommand(id, body.Reason ?? string.Empty), ct);
        return Ok(SuccessMessage("Đã thu hồi quyền đọc."));
    }

    /// <summary>Nhật ký truy cập tài liệu số.</summary>
    [HttpPost("logs/search")]
    [RequirePermission(PermissionCodes.DigitalAccessLogView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DigitalAccessLogRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<DigitalAccessLogRowDto>>>> SearchLogs(
        [FromBody] DigitalLogQueryRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchDigitalLogsQuery(request), ct);
        return Ok(Success(result));
    }

    // ---------------------------------------------------------------
    // Nhập xuất
    // ---------------------------------------------------------------

    /// <summary>Nhập hàng loạt tài liệu số từ một tệp nén ZIP.</summary>
    [HttpPost("import")]
    [RequirePermission(PermissionCodes.DigitalImport)]
    [RequestSizeLimit(500L * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<DigitalImportResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DigitalImportResultDto>>> Import(
        IFormFile file,
        [FromForm] Guid? collectionId,
        [FromForm] Domain.Enums.DigitalAccessLevel? accessLevel,
        [FromForm] bool allowDownload,
        [FromForm] bool allowPrint,
        [FromForm] bool dryRun,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Chưa chọn tệp nén."));
        }

        var result = await Mediator.Send(new ImportDigitalArchiveCommand
        {
            Archive = await ReadAllAsync(file, ct),
            CollectionId = collectionId,
            AccessLevel = accessLevel,
            AllowDownload = allowDownload,
            AllowPrint = allowPrint,
            DryRun = dryRun,
        }, ct);

        return Ok(Success(
            result,
            dryRun
                ? $"Kiểm tra xong {result.Total} tệp, {result.Failed} tệp có vấn đề."
                : $"Đã nhập {result.Success}/{result.Total} tệp."));
    }

    /// <summary>Tệp mẫu <c>metadata.xlsx</c> bỏ vào gói ZIP để khai nhan đề, mức truy cập, biểu ghi cho từng tệp.</summary>
    [HttpGet("import/template")]
    [RequirePermission(PermissionCodes.DigitalImport)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportTemplate(CancellationToken ct)
    {
        var file = await Mediator.Send(new GetDigitalImportTemplateQuery(), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Xuất gói tài liệu số kèm metadata Excel, Dublin Core và MARCXML.</summary>
    [HttpPost("export")]
    [RequirePermission(PermissionCodes.DigitalExport)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromBody] ExportDigitalArchiveQuery query, CancellationToken ct)
    {
        var file = await Mediator.Send(query, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    // ---------------------------------------------------------------
    // Xuất toàn bộ dữ liệu hệ thống (V.3, mục 4 E-HSMT)
    // ---------------------------------------------------------------

    /// <summary>Xếp một lượt xuất toàn bộ dữ liệu vào hàng đợi; gói ZIP dựng nền, xem tiến độ ở danh sách.</summary>
    [HttpPost("full-export")]
    [RequirePermission(PermissionCodes.ExchangeFullExport)]
    [ProducesResponseType(typeof(ApiResponse<FullSystemExportJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<FullSystemExportJobDto>>> QueueFullExport(CancellationToken ct)
    {
        var result = await Mediator.Send(new QueueFullSystemExportCommand(), ct);
        return Ok(Success(result, "Đã xếp lượt xuất toàn bộ dữ liệu vào hàng đợi. Gói sẽ hiện ở bảng bên dưới khi xong."));
    }

    /// <summary>Các lượt xuất toàn bộ gần nhất kèm tiến độ và số lượng từng phần.</summary>
    [HttpGet("full-export")]
    [RequirePermission(PermissionCodes.ExchangeFullExport)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FullSystemExportJobDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FullSystemExportJobDto>>>> FullExports(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetFullSystemExportsQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Tải gói bàn giao ZIP của một lượt đã hoàn tất.</summary>
    [HttpGet("full-export/{id:guid}/download")]
    [RequirePermission(PermissionCodes.ExchangeFullExport)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadFullExport(Guid id, CancellationToken ct)
    {
        var (content, fileName) = await Mediator.Send(new DownloadFullSystemExportQuery(id), ct);
        return File(content, "application/zip", fileName);
    }

    // ---------------------------------------------------------------
    // Báo cáo
    // ---------------------------------------------------------------

    /// <summary>Báo cáo số lượng tài liệu theo bộ sưu tập, định dạng và mức truy cập.</summary>
    [HttpPost("reports/inventory")]
    [RequirePermission(PermissionCodes.DigitalReportView)]
    [ProducesResponseType(typeof(ApiResponse<DigitalInventoryReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DigitalInventoryReportDto>>> InventoryReport(
        [FromBody] DigitalReportFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetDigitalInventoryReportQuery(filter), ct);
        return Ok(Success(result));
    }

    /// <summary>Báo cáo lượt xem và lượt tải.</summary>
    [HttpPost("reports/usage")]
    [RequirePermission(PermissionCodes.DigitalReportView)]
    [ProducesResponseType(typeof(ApiResponse<DigitalUsageReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DigitalUsageReportDto>>> UsageReport(
        [FromBody] DigitalReportFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetDigitalUsageReportQuery(filter), ct);
        return Ok(Success(result));
    }

    /// <summary>Báo cáo dung lượng lưu trữ đã dùng.</summary>
    [HttpGet("reports/storage")]
    [RequirePermission(PermissionCodes.DigitalReportView)]
    [ProducesResponseType(typeof(ApiResponse<DigitalStorageReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DigitalStorageReportDto>>> StorageReport(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetDigitalStorageReportQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Báo cáo yêu cầu đọc tài liệu hạn chế.</summary>
    [HttpPost("reports/requests")]
    [RequirePermission(PermissionCodes.DigitalReportView)]
    [ProducesResponseType(typeof(ApiResponse<DigitalRequestReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DigitalRequestReportDto>>> RequestReport(
        [FromBody] DigitalReportFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetDigitalRequestReportQuery(filter), ct);
        return Ok(Success(result));
    }

    /// <summary>Xuất một báo cáo tài liệu số ra PDF hoặc Excel.</summary>
    [HttpPost("reports/export")]
    [RequirePermission(PermissionCodes.DigitalReportView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportReport(
        [FromBody] ExportDigitalReportQuery query, CancellationToken ct)
    {
        var file = await Mediator.Send(query, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private static async Task<byte[]> ReadAllAsync(IFormFile file, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }
}

/// <summary>Thân yêu cầu cho các thao tác chỉ cần một lý do.</summary>
public class DigitalReasonRequest
{
    public string? Reason { get; set; }
}
