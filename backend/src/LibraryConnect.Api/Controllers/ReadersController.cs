using System.Text.Json;
using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Readers;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Phân hệ VI — Bạn đọc: hồ sơ, thẻ bạn đọc, lịch sử sử dụng thư viện, vi phạm, nhập xuất dữ liệu
/// và báo cáo thống kê.
/// </summary>
[Route("api/readers")]
[Tags("Bạn đọc")]
public class ReadersController : ApiControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // ---------------------------------------------------------------
    // VI.1 — Hồ sơ bạn đọc
    // ---------------------------------------------------------------

    /// <summary>
    /// Tra cứu bạn đọc theo số thẻ, mã sinh viên, họ tên (gõ không dấu vẫn ra), CCCD, email hoặc
    /// điện thoại; lọc theo loại bạn đọc, khoa, ngành, lớp, khóa và trạng thái thẻ.
    /// </summary>
    [HttpGet]
    [RequirePermission(PermissionCodes.ReaderView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReaderDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReaderDto>>>> Search(
        [FromQuery] ReaderListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchReadersQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Hồ sơ chi tiết kèm lịch sử cấp thẻ.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCodes.ReaderView)]
    [ProducesResponseType(typeof(ApiResponse<ReaderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReaderDetailDto>>> Get(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm bạn đọc. Bỏ trống số thẻ thì hệ thống sinh theo quy tắc đã cấu hình.</summary>
    [HttpPost]
    [RequirePermission(PermissionCodes.ReaderCreate)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] SaveReaderCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã lưu hồ sơ bạn đọc."));
    }

    /// <summary>Sửa hồ sơ bạn đọc.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionCodes.ReaderUpdate)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> Update(
        Guid id, [FromBody] SaveReaderCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu hồ sơ bạn đọc."));
    }

    /// <summary>Xóa hồ sơ bạn đọc khi không còn sách chưa trả và không còn nợ phí.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionCodes.ReaderDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> Delete(
        Guid id, [FromQuery] string? reason, CancellationToken ct)
    {
        await Mediator.Send(new DeleteReaderCommand(id, reason), ct);
        return Ok(SuccessMessage("Đã xóa hồ sơ bạn đọc."));
    }

    // ---------------------------------------------------------------
    // Ảnh chân dung
    // ---------------------------------------------------------------

    /// <summary>Tải ảnh chân dung lên — ảnh đã cắt trên màn hình hoặc chụp từ webcam.</summary>
    [HttpPost("{id:guid}/photo")]
    [RequirePermission(PermissionCodes.ReaderUpdate)]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> UploadPhoto(
        Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Chưa chọn ảnh."));
        }

        var content = await ReadAllAsync(file, ct);
        var objectName = await Mediator.Send(new UploadReaderPhotoCommand(id, content), ct);

        return Ok(Success(objectName, "Đã cập nhật ảnh bạn đọc."));
    }

    /// <summary>Ảnh chân dung của bạn đọc.</summary>
    [HttpGet("{id:guid}/photo")]
    [RequirePermission(PermissionCodes.ReaderView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPhoto(Guid id, CancellationToken ct)
    {
        var photo = await Mediator.Send(new GetReaderPhotoQuery(id), ct);
        return File(photo.Content, photo.ContentType);
    }

    /// <summary>Gỡ ảnh chân dung.</summary>
    [HttpDelete("{id:guid}/photo")]
    [RequirePermission(PermissionCodes.ReaderUpdate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeletePhoto(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteReaderPhotoCommand(id), ct);
        return Ok(SuccessMessage("Đã gỡ ảnh bạn đọc."));
    }

    /// <summary>Nhập ảnh hàng loạt từ tệp nén ZIP đặt tên theo mã sinh viên hoặc số thẻ.</summary>
    [HttpPost("photos/import")]
    [RequirePermission(PermissionCodes.ReaderImport)]
    [RequestSizeLimit(200 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<PhotoImportResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PhotoImportResultDto>>> ImportPhotos(
        IFormFile file, [FromQuery] bool dryRun, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Chưa chọn tệp nén."));
        }

        await using var stream = file.OpenReadStream();
        var result = await Mediator.Send(new ImportReaderPhotosCommand(stream, dryRun), ct);

        return Ok(Success(result, dryRun
            ? $"Khớp được {result.Matched} ảnh, chưa ghi vào hồ sơ."
            : $"Đã cập nhật ảnh cho {result.Matched} bạn đọc."));
    }

    // ---------------------------------------------------------------
    // VI.1 — Thao tác trên thẻ và trạng thái
    // ---------------------------------------------------------------

    /// <summary>Gia hạn thẻ cho một người, một danh sách chọn, hoặc toàn bộ kết quả lọc.</summary>
    [HttpPost("cards/extend")]
    [RequirePermission(PermissionCodes.ReaderExtendCard)]
    [ProducesResponseType(typeof(ApiResponse<BulkResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BulkResultDto>>> ExtendCards(
        [FromBody] ExtendReaderCardsCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, $"Đã gia hạn {result.Succeeded} thẻ."));
    }

    /// <summary>Tạm khóa hoặc mở khóa thẻ, kèm lý do.</summary>
    [HttpPost("lock")]
    [RequirePermission(PermissionCodes.ReaderLock)]
    [ProducesResponseType(typeof(ApiResponse<BulkResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BulkResultDto>>> SetLock(
        [FromBody] SetReaderLockCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, command.Locked
            ? $"Đã tạm khóa {result.Succeeded} thẻ."
            : $"Đã mở khóa {result.Succeeded} thẻ."));
    }

    /// <summary>Cấp lại thẻ; thẻ cũ được giữ lại trong lịch sử.</summary>
    [HttpPost("{id:guid}/cards/reissue")]
    [RequirePermission(PermissionCodes.ReaderExtendCard)]
    [ProducesResponseType(typeof(ApiResponse<ReaderCardDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReaderCardDto>>> ReissueCard(
        Guid id, [FromBody] ReissueReaderCardCommand command, CancellationToken ct)
    {
        command.ReaderId = id;
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, $"Đã cấp lại thẻ số {result.CardNumber}."));
    }

    /// <summary>
    /// In giấy xác nhận trả sách (VII.4) cho một bạn đọc theo mẫu biểu dùng chung. Đặt ở đây với quyền
    /// xem bạn đọc, vì cán bộ bạn đọc không có quyền in chứng từ bổ sung mà lối in chung đòi hỏi.
    /// </summary>
    [HttpGet("{id:guid}/clearance/print")]
    [RequirePermission(PermissionCodes.ReaderView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PrintClearance(Guid id, [FromQuery] Guid? templateId, CancellationToken ct)
    {
        var clearance = await Mediator.Send(new GetReaderClearanceQuery(id), ct);

        var file = await Mediator.Send(
            new Application.Features.Acquisition.PrintFormCommand(
                Application.Features.Acquisition.FormTypes.Clearance, clearance.CardNumber, templateId), ct);

        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Chuyển trạng thái ra trường; bạn đọc còn sách hoặc nợ phí bị giữ lại kèm lý do.</summary>
    [HttpPost("graduate")]
    [RequirePermission(PermissionCodes.ReaderUpdate)]
    [ProducesResponseType(typeof(ApiResponse<BulkResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BulkResultDto>>> Graduate(
        [FromBody] GraduateReadersCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, result.Skipped == 0
            ? $"Đã chuyển {result.Succeeded} bạn đọc sang trạng thái ra trường."
            : $"Đã chuyển {result.Succeeded} bạn đọc; {result.Skipped} người còn công nợ nên chưa chuyển."));
    }

    /// <summary>Kiểm tra công nợ của một bạn đọc — căn cứ xác nhận trả sách khi ra trường.</summary>
    [HttpGet("{id:guid}/clearance")]
    [RequirePermission(PermissionCodes.ReaderView)]
    [ProducesResponseType(typeof(ApiResponse<ReaderClearanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReaderClearanceDto>>> GetClearance(
        Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderClearanceQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Đặt lại mật khẩu tra cứu của bạn đọc.</summary>
    [HttpPost("{id:guid}/reset-password")]
    [RequirePermission(PermissionCodes.ReaderResetPassword)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword(
        Guid id, [FromBody] ResetReaderPasswordRequest? body, CancellationToken ct)
    {
        var password = await Mediator.Send(new ResetReaderPasswordCommand(id, body?.NewPassword), ct);
        return Ok(Success(password, "Đã đặt lại mật khẩu. Bạn đọc phải đổi ở lần đăng nhập đầu tiên."));
    }

    // ---------------------------------------------------------------
    // VI.1 — Tab lịch sử
    // ---------------------------------------------------------------

    /// <summary>Sách đang mượn hoặc toàn bộ lịch sử mượn trả của bạn đọc.</summary>
    [HttpGet("{id:guid}/loans")]
    [RequirePermission(PermissionCodes.ReaderView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReaderLoanDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReaderLoanDto>>>> GetLoans(
        Guid id, [FromQuery] bool currentOnly, [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderLoansQuery(id, currentOnly, request), ct);
        return Ok(Success(result));
    }

    /// <summary>Tiền phạt của bạn đọc.</summary>
    [HttpGet("{id:guid}/fines")]
    [RequirePermission(PermissionCodes.ReaderView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReaderFineDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReaderFineDto>>>> GetFines(
        Guid id, [FromQuery] bool outstandingOnly, [FromQuery] PagedRequestDefault request,
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderFinesQuery(id, outstandingOnly, request), ct);
        return Ok(Success(result));
    }

    /// <summary>Lượt vào thư viện.</summary>
    [HttpGet("{id:guid}/visits")]
    [RequirePermission(PermissionCodes.ReaderView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReaderVisitDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReaderVisitDto>>>> GetVisits(
        Guid id, [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderVisitsQuery(id, request), ct);
        return Ok(Success(result));
    }

    /// <summary>Tài liệu số bạn đọc đã xem hoặc tải.</summary>
    [HttpGet("{id:guid}/digital-access")]
    [RequirePermission(PermissionCodes.ReaderView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReaderDigitalAccessDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReaderDigitalAccessDto>>>> GetDigitalAccess(
        Guid id, [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderDigitalAccessQuery(id, request), ct);
        return Ok(Success(result));
    }

    /// <summary>Vi phạm của bạn đọc.</summary>
    [HttpGet("{id:guid}/violations")]
    [RequirePermission(PermissionCodes.ReaderView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReaderViolationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReaderViolationDto>>>> GetViolations(
        Guid id, [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderViolationsQuery(id, request), ct);
        return Ok(Success(result));
    }

    /// <summary>Ghi nhận hoặc sửa một vi phạm.</summary>
    [HttpPost("{id:guid}/violations")]
    [RequirePermission(PermissionCodes.ReaderViolationManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> SaveViolation(
        Guid id, [FromBody] SaveReaderViolationCommand command, CancellationToken ct)
    {
        command.ReaderId = id;
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, "Đã lưu vi phạm."));
    }

    /// <summary>Xóa một vi phạm ghi nhầm.</summary>
    [HttpDelete("violations/{violationId:guid}")]
    [RequirePermission(PermissionCodes.ReaderViolationManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteViolation(Guid violationId, CancellationToken ct)
    {
        await Mediator.Send(new DeleteReaderViolationCommand(violationId), ct);
        return Ok(SuccessMessage("Đã xóa vi phạm."));
    }

    // ---------------------------------------------------------------
    // VI.2 — Mẫu thẻ và in thẻ
    // ---------------------------------------------------------------

    /// <summary>Danh sách mẫu thẻ bạn đọc.</summary>
    [HttpGet("card-templates")]
    [RequirePermission(PermissionCodes.ReaderPrintCard)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReaderCardTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReaderCardTemplateDto>>>> GetCardTemplates(
        [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderCardTemplatesQuery(includeInactive), ct);
        return Ok(Success(result));
    }

    /// <summary>Các trường kéo được lên thẻ, để màn hình thiết kế đổ vào danh sách chọn.</summary>
    [HttpGet("card-templates/fields")]
    [RequirePermission(PermissionCodes.ReaderPrintCard)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CardFieldOptionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CardFieldOptionDto>>>> GetCardFields(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderCardFieldsQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Lưu mẫu thẻ: khổ thẻ, bố cục mặt trước và mặt sau.</summary>
    [HttpPost("card-templates")]
    [RequirePermission(PermissionCodes.ReaderCardTemplateManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> SaveCardTemplate(
        [FromBody] SaveReaderCardTemplateCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu mẫu thẻ."));
    }

    /// <summary>Xóa mẫu thẻ chưa dùng để in lần nào.</summary>
    [HttpDelete("card-templates/{templateId:guid}")]
    [RequirePermission(PermissionCodes.ReaderCardTemplateManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> DeleteCardTemplate(Guid templateId, CancellationToken ct)
    {
        await Mediator.Send(new DeleteReaderCardTemplateCommand(templateId), ct);
        return Ok(SuccessMessage("Đã xóa mẫu thẻ."));
    }

    /// <summary>
    /// In thẻ ra PDF. Xem trước thì không tăng số lần in; in thật thì mỗi thẻ được cộng một lượt.
    /// </summary>
    [HttpPost("cards/print")]
    [RequirePermission(PermissionCodes.ReaderPrintCard)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> PrintCards(
        [FromBody] PrintReaderCardsCommand command, CancellationToken ct)
    {
        var file = await Mediator.Send(command, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    // ---------------------------------------------------------------
    // VI.4 — Nhập, xuất và đồng bộ dữ liệu
    // ---------------------------------------------------------------

    /// <summary>Tải tệp Excel mẫu kèm sheet hướng dẫn từng cột.</summary>
    [HttpGet("import/template")]
    [RequirePermission(PermissionCodes.ReaderImport)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImportTemplate(CancellationToken ct)
    {
        var file = await Mediator.Send(new GetReaderImportTemplateQuery(), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Ánh xạ cột đã lưu cho lần nhập trước.</summary>
    [HttpGet("import/mapping")]
    [RequirePermission(PermissionCodes.ReaderImport)]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, string>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Dictionary<string, string>>>> GetImportMapping(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderImportMappingQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Lưu ánh xạ cột để lần nhập sau khỏi làm lại.</summary>
    [HttpPut("import/mapping")]
    [RequirePermission(PermissionCodes.ReaderImport)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> SaveImportMapping(
        [FromBody] Dictionary<string, string> mapping, CancellationToken ct)
    {
        await Mediator.Send(new SaveReaderImportMappingCommand(mapping), ct);
        return Ok(SuccessMessage("Đã lưu ánh xạ cột."));
    }

    /// <summary>Kiểm tra tệp trước khi nhập: trả bảng lỗi theo từng dòng, không ghi gì vào hệ thống.</summary>
    [HttpPost("import/validate")]
    [RequirePermission(PermissionCodes.ReaderImport)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<ReaderImportPreviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReaderImportPreviewDto>>> ValidateImport(
        IFormFile file, [FromForm] string? options, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Chưa chọn tệp."));
        }

        await using var stream = file.OpenReadStream();

        var result = await Mediator.Send(
            new ValidateReaderImportQuery(stream, file.FileName, ParseOptions(options)), ct);

        return Ok(Success(result, result.ErrorRows == 0
            ? $"Tệp hợp lệ: {result.ValidRows} dòng sẵn sàng nhập."
            : $"Có {result.ErrorRows} dòng lỗi trong tổng số {result.TotalRows} dòng."));
    }

    /// <summary>Nhập bạn đọc từ Excel, chạy nền. Trả về mã đợt nhập để theo dõi tiến độ.</summary>
    [HttpPost("import")]
    [RequirePermission(PermissionCodes.ReaderImport)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> StartImport(
        IFormFile file, [FromForm] string? options, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Chưa chọn tệp."));
        }

        var content = await ReadAllAsync(file, ct);

        var batchId = await Mediator.Send(
            new StartReaderImportCommand(content, file.FileName, ParseOptions(options)), ct);

        return Ok(Success(batchId, "Đã xếp hàng đợt nhập. Theo dõi tiến độ ở danh sách bên dưới."));
    }

    /// <summary>
    /// Kiểm tra lại (<c>dryRun</c>) hoặc nhập thật những dòng cán bộ đã sửa ngay trên bảng lỗi, không
    /// cần sửa tệp Excel. Số dòng giữ nguyên như trong tệp để đối chiếu.
    /// </summary>
    [HttpPost("import/rows")]
    [RequirePermission(PermissionCodes.ReaderImport)]
    [ProducesResponseType(typeof(ApiResponse<ReaderImportRowsResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReaderImportRowsResultDto>>> ImportRows(
        [FromBody] ImportReaderRowsCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, result.DryRun
            ? (result.ErrorRows == 0
                ? $"{result.TotalRows} dòng đã hợp lệ, có thể nhập."
                : $"Còn {result.ErrorRows} dòng lỗi.")
            : $"Đã nhập {result.Created + result.Updated} dòng" +
              (result.ErrorRows > 0 ? $", còn {result.ErrorRows} dòng lỗi." : ".")));
    }

    /// <summary>Danh sách các đợt nhập bạn đọc.</summary>
    [HttpGet("import/batches")]
    [RequirePermission(PermissionCodes.ReaderImport)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReaderImportBatchDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReaderImportBatchDto>>>> GetImportBatches(
        [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderImportBatchesQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Tiến độ và lỗi của một đợt nhập.</summary>
    [HttpGet("import/batches/{batchId:guid}")]
    [RequirePermission(PermissionCodes.ReaderImport)]
    [ProducesResponseType(typeof(ApiResponse<ReaderImportBatchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReaderImportBatchDto>>> GetImportBatch(
        Guid batchId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderImportBatchQuery(batchId), ct);
        return Ok(Success(result));
    }

    /// <summary>Tải nhật ký lỗi của một đợt nhập ra Excel.</summary>
    [HttpGet("import/batches/{batchId:guid}/errors")]
    [RequirePermission(PermissionCodes.ReaderImport)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImportErrors(Guid batchId, CancellationToken ct)
    {
        var file = await Mediator.Send(new GetReaderImportErrorsQuery(batchId), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Xuất danh sách bạn đọc ra Excel theo đúng bộ lọc đang xem.</summary>
    [HttpGet("export")]
    [RequirePermission(PermissionCodes.ReaderExport)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export([FromQuery] ReaderListRequest request, CancellationToken ct)
    {
        var file = await Mediator.Send(new ExportReadersQuery(request), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>
    /// Đồng bộ bạn đọc từ hệ thống quản lý đào tạo. Bản ghi khớp theo mã sinh viên thì cập nhật,
    /// chưa có thì tạo mới. Ánh xạ tên trường lấy từ cấu hình, gửi kèm cũng được.
    /// </summary>
    [HttpPost("sync")]
    [RequirePermission(PermissionCodes.ReaderImport)]
    [ProducesResponseType(typeof(ApiResponse<ReaderSyncResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReaderSyncResultDto>>> Sync(
        [FromBody] SyncReadersCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, command.DryRun
            ? $"Kiểm tra xong: {result.Created} thêm mới, {result.Updated} cập nhật, {result.ErrorItems} lỗi."
            : $"Đã đồng bộ: thêm {result.Created}, cập nhật {result.Updated}, lỗi {result.ErrorItems}."));
    }

    /// <summary>Ánh xạ tên trường của hệ thống đào tạo.</summary>
    [HttpGet("sync/mapping")]
    [RequirePermission(PermissionCodes.ReaderImport)]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, string>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Dictionary<string, string>>>> GetSyncMapping(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderSyncMappingQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Lưu ánh xạ tên trường của hệ thống đào tạo.</summary>
    [HttpPut("sync/mapping")]
    [RequirePermission(PermissionCodes.ReaderImport)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> SaveSyncMapping(
        [FromBody] Dictionary<string, string> mapping, CancellationToken ct)
    {
        await Mediator.Send(new SaveReaderSyncMappingCommand(mapping), ct);
        return Ok(SuccessMessage("Đã lưu ánh xạ trường đồng bộ."));
    }

    // ---------------------------------------------------------------
    // VI.5 — Báo cáo
    // ---------------------------------------------------------------

    /// <summary>Số lượng bạn đọc theo loại, khoa, ngành, khóa, lớp, trạng thái hoặc giới tính.</summary>
    [HttpGet("reports/count")]
    [RequirePermission(PermissionCodes.ReaderReportView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReaderReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReaderReportRowDto>>>> GetCountReport(
        [FromQuery] ReaderReportDimension dimension, [FromQuery] ReaderReportFilter filter,
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderCountReportQuery(dimension, filter), ct);
        return Ok(Success(result));
    }

    /// <summary>Bạn đọc đăng ký mới theo ngày, tháng, quý hoặc năm.</summary>
    [HttpGet("reports/registrations")]
    [RequirePermission(PermissionCodes.ReaderReportView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReaderTimeRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReaderTimeRowDto>>>> GetRegistrationReport(
        [FromQuery] ReaderTimeGrouping grouping, [FromQuery] ReaderReportFilter filter,
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderRegistrationReportQuery(grouping, filter), ct);
        return Ok(Success(result));
    }

    /// <summary>Thẻ sắp hết hạn và đã hết hạn.</summary>
    [HttpGet("reports/expiring-cards")]
    [RequirePermission(PermissionCodes.ReaderReportView)]
    [ProducesResponseType(typeof(ApiResponse<ExpiringCardsReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ExpiringCardsReportDto>>> GetExpiringCards(
        [FromQuery] int withinDays, [FromQuery] ReaderReportFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetExpiringCardsReportQuery(withinDays, filter), ct);
        return Ok(Success(result));
    }

    /// <summary>Bạn đọc tích cực nhất, hoặc bạn đọc chưa từng mượn tài liệu.</summary>
    [HttpGet("reports/activity")]
    [RequirePermission(PermissionCodes.ReaderReportView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReaderActivityRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReaderActivityRowDto>>>> GetActivityReport(
        [FromQuery] bool neverBorrowed, [FromQuery] int top, [FromQuery] ReaderReportFilter filter,
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReaderActivityReportQuery(neverBorrowed, top, filter), ct);
        return Ok(Success(result));
    }

    /// <summary>Xuất báo cáo bạn đọc đang xem ra Excel hoặc PDF.</summary>
    [HttpPost("reports/export")]
    [RequirePermission(PermissionCodes.ReaderReportView)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportReport(
        [FromBody] ExportReaderReportQuery query, CancellationToken ct)
    {
        var file = await Mediator.Send(query, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private static ReaderImportOptions ParseOptions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ReaderImportOptions();
        }

        try
        {
            return JsonSerializer.Deserialize<ReaderImportOptions>(json, JsonOptions)
                   ?? new ReaderImportOptions();
        }
        catch (JsonException)
        {
            return new ReaderImportOptions();
        }
    }

    private static async Task<byte[]> ReadAllAsync(IFormFile file, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }
}

public class ResetReaderPasswordRequest
{
    public string? NewPassword { get; set; }
}
