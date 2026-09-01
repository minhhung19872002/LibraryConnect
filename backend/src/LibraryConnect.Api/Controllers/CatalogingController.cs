using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Cataloging;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Phân hệ II — Biên mục: biểu ghi thư mục, đăng ký cá biệt, giá trị ngầm định và mẫu biên mục.
/// </summary>
[Route("api/cataloging")]
[Tags("Biên mục")]
public class CatalogingController : ApiControllerBase
{
    // ---------------------------------------------------------------
    // Biểu ghi thư mục
    // ---------------------------------------------------------------

    /// <summary>Danh sách biểu ghi, lọc và phân trang phía máy chủ.</summary>
    [HttpGet("bibs")]
    [RequirePermission(PermissionCodes.CatalogBibView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<BibListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<BibListItemDto>>>> GetBibs(
        [FromQuery] BibListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetBibRecordsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một biểu ghi: MARC thô, mô tả ISBD và các liên kết danh mục.</summary>
    [HttpGet("bibs/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogBibView)]
    [ProducesResponseType(typeof(ApiResponse<BibDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BibDetailDto>>> GetBib(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetBibRecordQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>
    /// Khung biểu ghi mới, đã điền sẵn theo mẫu biên mục và bảng giá trị ngầm định của dạng tài liệu.
    /// </summary>
    [HttpGet("bibs/new")]
    [RequirePermission(PermissionCodes.CatalogBibCreate)]
    [ProducesResponseType(typeof(ApiResponse<NewBibRecordDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<NewBibRecordDto>>> GetNewBib(
        [FromQuery] Guid? documentTypeId, [FromQuery] Guid? templateId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetNewBibRecordQuery(documentTypeId, templateId), ct);
        return Ok(Success(result));
    }

    /// <summary>Tạo biểu ghi mới.</summary>
    [HttpPost("bibs")]
    [RequirePermission(PermissionCodes.CatalogBibCreate)]
    [ProducesResponseType(typeof(ApiResponse<SaveBibResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SaveBibResultDto>>> CreateBib(
        [FromBody] SaveBibRecordCommand command, CancellationToken ct)
    {
        command.Id = null;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, $"Đã lưu biểu ghi {result.ControlNumber}."));
    }

    /// <summary>Cập nhật biểu ghi. Phiên bản trước được lưu lại trước khi ghi đè.</summary>
    [HttpPut("bibs/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogBibUpdate)]
    [ProducesResponseType(typeof(ApiResponse<SaveBibResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SaveBibResultDto>>> UpdateBib(
        Guid id, [FromBody] SaveBibRecordCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã cập nhật biểu ghi."));
    }

    /// <summary>Xóa mềm một biểu ghi. Biểu ghi còn đăng ký cá biệt hoặc tài liệu số thì không xóa được.</summary>
    [HttpDelete("bibs/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogBibDelete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteBib(
        Guid id, [FromBody] DeleteBibRequest request, CancellationToken ct)
    {
        await Mediator.Send(new DeleteBibRecordCommand(id, request.Reason), ct);
        return Ok(Success<object?>(null, "Đã xóa biểu ghi."));
    }

    // ---------------------------------------------------------------
    // Lịch sử phiên bản
    // ---------------------------------------------------------------

    /// <summary>Các phiên bản đã lưu của biểu ghi.</summary>
    [HttpGet("bibs/{id:guid}/versions")]
    [RequirePermission(PermissionCodes.CatalogBibView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BibVersionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BibVersionDto>>>> GetVersions(
        Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetBibVersionsQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>So sánh một phiên bản với nội dung hiện tại, hoặc với một phiên bản khác.</summary>
    [HttpGet("bibs/{id:guid}/versions/{versionId:guid}/diff")]
    [RequirePermission(PermissionCodes.CatalogBibView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MarcDiffLineDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MarcDiffLineDto>>>> GetVersionDiff(
        Guid id, Guid versionId, [FromQuery] Guid? compareTo, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetBibVersionDiffQuery(id, versionId, compareTo), ct);
        return Ok(Success(result));
    }

    /// <summary>Khôi phục biểu ghi về một phiên bản cũ.</summary>
    [HttpPost("bibs/{id:guid}/versions/{versionId:guid}/restore")]
    [RequirePermission(PermissionCodes.CatalogBibVersionRestore)]
    [ProducesResponseType(typeof(ApiResponse<SaveBibResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SaveBibResultDto>>> RestoreVersion(
        Guid id, Guid versionId, CancellationToken ct)
    {
        var result = await Mediator.Send(new RestoreBibVersionCommand(id, versionId), ct);
        return Ok(Success(result, "Đã khôi phục biểu ghi về phiên bản đã chọn."));
    }

    // ---------------------------------------------------------------
    // Đăng ký cá biệt
    // ---------------------------------------------------------------

    /// <summary>Danh sách đăng ký cá biệt của một biểu ghi.</summary>
    [HttpGet("bibs/{id:guid}/items")]
    [RequirePermission(PermissionCodes.CatalogBibView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ItemDto>>>> GetItems(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetBibItemsQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Tạo đăng ký cá biệt cho biểu ghi; mã vạch và số ĐKCB do hệ thống sinh.</summary>
    [HttpPost("bibs/{id:guid}/items")]
    [RequirePermission(PermissionCodes.AcqItemCreate)]
    [ProducesResponseType(typeof(ApiResponse<CreateItemsResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CreateItemsResultDto>>> CreateItems(
        Guid id, [FromBody] CreateBibItemsCommand command, CancellationToken ct)
    {
        command.BibId = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, $"Đã tạo {result.Created} đăng ký cá biệt."));
    }

    /// <summary>Sửa một đăng ký cá biệt.</summary>
    [HttpPut("items/{itemId:guid}")]
    [RequirePermission(PermissionCodes.AcqItemUpdate)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateItem(
        Guid itemId, [FromBody] UpdateItemCommand command, CancellationToken ct)
    {
        command.Id = itemId;
        await Mediator.Send(command, ct);
        return Ok(Success<object?>(null, "Đã cập nhật đăng ký cá biệt."));
    }

    /// <summary>Xóa mềm một đăng ký cá biệt.</summary>
    [HttpDelete("items/{itemId:guid}")]
    [RequirePermission(PermissionCodes.AcqItemDelete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteItem(
        Guid itemId, [FromBody] DeleteBibRequest request, CancellationToken ct)
    {
        await Mediator.Send(new DeleteItemCommand(itemId, request.Reason), ct);
        return Ok(Success<object?>(null, "Đã xóa đăng ký cá biệt."));
    }

    // ---------------------------------------------------------------
    // Giá trị ngầm định của trường MARC (II.1)
    // ---------------------------------------------------------------

    [HttpGet("marc-defaults")]
    [RequirePermission(PermissionCodes.CatalogDefaultValueManage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MarcFieldDefaultDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MarcFieldDefaultDto>>>> GetMarcDefaults(
        [FromQuery] Guid? documentTypeId, [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMarcDefaultsQuery(documentTypeId, includeInactive), ct);
        return Ok(Success(result));
    }

    [HttpPost("marc-defaults")]
    [RequirePermission(PermissionCodes.CatalogDefaultValueManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateMarcDefault(
        [FromBody] SaveMarcDefaultCommand command, CancellationToken ct)
    {
        command.Id = null;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã thêm giá trị ngầm định."));
    }

    [HttpPut("marc-defaults/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogDefaultValueManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateMarcDefault(
        Guid id, [FromBody] SaveMarcDefaultCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã cập nhật giá trị ngầm định."));
    }

    [HttpDelete("marc-defaults/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogDefaultValueManage)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteMarcDefault(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteMarcDefaultCommand(id), ct);
        return Ok(Success<object?>(null, "Đã xóa giá trị ngầm định."));
    }

    // ---------------------------------------------------------------
    // Mẫu biên mục (II.5)
    // ---------------------------------------------------------------

    [HttpGet("templates")]
    [RequirePermission(PermissionCodes.CatalogTemplateView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MarcTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MarcTemplateDto>>>> GetTemplates(
        [FromQuery] Guid? documentTypeId, [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMarcTemplatesQuery(documentTypeId, includeInactive), ct);
        return Ok(Success(result));
    }

    [HttpPost("templates")]
    [RequirePermission(PermissionCodes.CatalogTemplateManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateTemplate(
        [FromBody] SaveMarcTemplateCommand command, CancellationToken ct)
    {
        command.Id = null;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã thêm mẫu biên mục."));
    }

    [HttpPut("templates/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogTemplateManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateTemplate(
        Guid id, [FromBody] SaveMarcTemplateCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã cập nhật mẫu biên mục."));
    }

    [HttpDelete("templates/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogTemplateManage)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteTemplate(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteMarcTemplateCommand(id), ct);
        return Ok(Success<object?>(null, "Đã xóa mẫu biên mục."));
    }

    // ---------------------------------------------------------------
    // Nhập và xuất biểu ghi (II.6)
    // ---------------------------------------------------------------

    /// <summary>
    /// Bước xem trước: đọc tệp .mrc hoặc .xml, kiểm tra từng biểu ghi và đối chiếu trùng với biểu ghi
    /// đã có. Không ghi gì vào cơ sở dữ liệu.
    /// </summary>
    [HttpPost("import/preview")]
    [RequirePermission(PermissionCodes.CatalogBibImport)]
    [ProducesResponseType(typeof(ApiResponse<BibImportPreviewDto>), StatusCodes.Status200OK)]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<BibImportPreviewDto>>> PreviewImport(
        IFormFile file, [FromQuery] DuplicateMatchBy matchBy, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Vui lòng chọn tệp biểu ghi cần nhập."));
        }

        var content = await ReadAllAsync(file, ct);
        var result = await Mediator.Send(new PreviewBibImportCommand(content, file.FileName, matchBy), ct);

        var message = result.DuplicateCount == 0
            ? $"Đọc được {result.TotalRecords} biểu ghi, không có biểu ghi nào trùng."
            : $"Đọc được {result.TotalRecords} biểu ghi, {result.DuplicateCount} biểu ghi trùng với dữ liệu đã có.";

        return Ok(Success(result, message));
    }

    /// <summary>
    /// Bắt đầu nhập thật. Tác vụ chạy nền; dùng mã trả về để theo dõi tiến độ.
    /// </summary>
    [HttpPost("import")]
    [RequirePermission(PermissionCodes.CatalogBibImport)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<Guid>>> StartImport(
        IFormFile file, [FromForm] string options, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Vui lòng chọn tệp biểu ghi cần nhập."));
        }

        BibImportOptions parsed;

        try
        {
            parsed = System.Text.Json.JsonSerializer.Deserialize<BibImportOptions>(
                         string.IsNullOrWhiteSpace(options) ? "{}" : options,
                         ImportOptionsJson)
                     ?? new BibImportOptions();
        }
        catch (System.Text.Json.JsonException exception)
        {
            return BadRequest(ApiResponse.Fail($"Tùy chọn nhập dữ liệu không đọc được: {exception.Message}"));
        }

        var content = await ReadAllAsync(file, ct);
        var jobId = await Mediator.Send(new StartBibImportCommand(content, file.FileName, parsed), ct);

        return Ok(Success(jobId, "Đã bắt đầu nhập dữ liệu. Tiến độ hiển thị bên dưới."));
    }

    /// <summary>Danh sách tác vụ nhập và xuất gần đây.</summary>
    [HttpGet("import/jobs")]
    [RequirePermission(PermissionCodes.CatalogBibImport)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ImportJobDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ImportJobDto>>>> GetImportJobs(
        [FromQuery] int take, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetImportJobsQuery(take <= 0 ? 30 : take), ct);
        return Ok(Success(result));
    }

    /// <summary>Tiến độ và kết quả của một tác vụ nhập.</summary>
    [HttpGet("import/jobs/{jobId:guid}")]
    [RequirePermission(PermissionCodes.CatalogBibImport)]
    [ProducesResponseType(typeof(ApiResponse<ImportJobDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ImportJobDto>>> GetImportJob(Guid jobId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetImportJobQuery(jobId), ct);
        return Ok(Success(result));
    }

    /// <summary>
    /// Xuất biểu ghi ra tệp trao đổi: theo danh sách đã tick chọn, hoặc theo đúng bộ lọc đang dùng
    /// trên màn hình danh sách.
    /// </summary>
    [HttpPost("export")]
    [RequirePermission(PermissionCodes.CatalogBibExport)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export([FromBody] ExportBibsRequest request, CancellationToken ct)
    {
        var file = await Mediator.Send(
            new ExportBibRecordsCommand(request.Filter ?? new BibListRequest(), request.Ids, request.Format), ct);

        // Biểu ghi nào không biểu diễn nổi bằng ISO 2709 thì báo ngay trên tiêu đề phản hồi. Trả về
        // một tệp thiếu vài biểu ghi mà không nói gì là để cán bộ đối chiếu số lượng lệch mãi không
        // hiểu vì sao; còn đánh đổ cả lượt xuất vì một biểu ghi thì mất luôn 7.674 biểu ghi lành.
        if (file.Skipped.Count > 0)
        {
            Response.Headers["X-LibraryConnect-Bo-Qua"] = file.Skipped.Count.ToString();
            Response.Headers["X-LibraryConnect-Bo-Qua-Ly-Do"] =
                System.Net.WebUtility.UrlEncode(file.Skipped[0].Reason);
        }

        return File(file.Content, file.ContentType, file.FileName);
    }

    // ---------------------------------------------------------------
    // Ảnh bìa
    // ---------------------------------------------------------------

    /// <summary>
    /// Tra ảnh bìa thật cho một biểu ghi ở nguồn ngoài.
    /// </summary>
    /// <remarks>
    /// Bốn lớp, dừng ở lớp đầu tiên có kết quả: ảnh cán bộ đã tải lên → địa chỉ ảnh trong trường
    /// 856 → Google Books theo ISBN → Open Library theo ISBN. Không lớp nào có thì biểu ghi vẫn
    /// hiện bìa dựng sẵn từ dữ liệu thư mục.
    /// </remarks>
    [HttpPost("bibs/{id:guid}/cover/lookup")]
    [RequirePermission(PermissionCodes.CatalogBibUpdate)]
    [ProducesResponseType(typeof(ApiResponse<CoverLookupOutcome>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CoverLookupOutcome>>> LookupCover(
        Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new LookupBibCoverCommand(id), ct);

        return Ok(Success(result, result.Found
            ? $"Đã lấy được ảnh bìa từ {result.Source}."
            : result.Reason ?? "Không tìm thấy ảnh bìa."));
    }

    /// <summary>Cán bộ tự tải ảnh bìa lên. Ảnh này không bao giờ bị lượt tra tự động ghi đè.</summary>
    [HttpPost("bibs/{id:guid}/cover")]
    [RequirePermission(PermissionCodes.CatalogBibUpdate)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<string>>> UploadCover(
        Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Chưa chọn tệp ảnh."));
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);

        var url = await Mediator.Send(
            new UploadBibCoverCommand(id, buffer.ToArray(), file.FileName, file.ContentType), ct);

        return Ok(Success(url, "Đã cập nhật ảnh bìa."));
    }

    /// <summary>Mở một lượt tra ảnh bìa hàng loạt cho những biểu ghi chưa có ảnh.</summary>
    [HttpPost("covers/lookup-batch")]
    [RequirePermission(PermissionCodes.CatalogBibUpdate)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<Guid>>> LookupCoversBatch(
        [FromQuery] int maxRecords, CancellationToken ct)
    {
        var jobId = await Mediator.Send(
            new StartCoverLookupCommand(maxRecords <= 0 ? 500 : maxRecords), ct);

        return Ok(Success(jobId,
            "Đã xếp lượt tra ảnh bìa vào hàng đợi. Tiến độ xem ở phần Nhập xuất dữ liệu."));
    }

    // ---------------------------------------------------------------
    // Danh mục tự tạo từ trường MARC (II.9)
    // ---------------------------------------------------------------

    /// <summary>Danh sách các danh mục tự tạo đã khai báo.</summary>
    [HttpGet("custom-indexes")]
    [RequirePermission(PermissionCodes.CatalogCustomIndexManage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomIndexDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomIndexDto>>>> GetCustomIndexes(
        [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCustomIndexesQuery(includeInactive), ct);
        return Ok(Success(result));
    }

    /// <summary>Các giá trị đã rút được của một danh mục tự tạo.</summary>
    [HttpGet("custom-indexes/{id:guid}/values")]
    [RequirePermission(PermissionCodes.CatalogCustomIndexManage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomIndexValueDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomIndexValueDto>>>> GetCustomIndexValues(
        Guid id, [FromQuery] string? keyword, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCustomIndexValuesQuery(id, keyword), ct);
        return Ok(Success(result));
    }

    /// <summary>Khai báo một danh mục mới bằng cách chỉ định trường và trường con MARC nguồn.</summary>
    [HttpPost("custom-indexes")]
    [RequirePermission(PermissionCodes.CatalogCustomIndexManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateCustomIndex(
        [FromBody] SaveCustomIndexCommand command, CancellationToken ct)
    {
        command.Id = null;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã khai báo danh mục tự tạo. Bấm Quét để rút giá trị từ biểu ghi."));
    }

    [HttpPut("custom-indexes/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogCustomIndexManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateCustomIndex(
        Guid id, [FromBody] SaveCustomIndexCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã cập nhật danh mục tự tạo."));
    }

    [HttpDelete("custom-indexes/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogCustomIndexManage)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCustomIndex(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteCustomIndexCommand(id), ct);
        return Ok(Success<object?>(null, "Đã xóa danh mục tự tạo."));
    }

    /// <summary>Quét toàn bộ biểu ghi để rút giá trị và dựng lại liên kết dùng cho bộ lọc tra cứu.</summary>
    [HttpPost("custom-indexes/{id:guid}/harvest")]
    [RequirePermission(PermissionCodes.CatalogCustomIndexManage)]
    [ProducesResponseType(typeof(ApiResponse<HarvestResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<HarvestResultDto>>> HarvestCustomIndex(
        Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new HarvestCustomIndexCommand(id), ct);

        return Ok(Success(result,
            $"Quét xong: {result.DistinctValues} giá trị, trong đó {result.NewValues} giá trị mới."));
    }

    /// <summary>Gộp nhiều cách viết của cùng một giá trị về một giá trị duy nhất.</summary>
    [HttpPost("custom-indexes/{id:guid}/merge")]
    [RequirePermission(PermissionCodes.CatalogCustomIndexManage)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> MergeCustomIndexValues(
        Guid id, [FromBody] MergeCustomIndexRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new MergeCustomIndexValuesCommand(id, request.KeepId, request.MergeIds), ct);

        return Ok(Success(result, $"Đã gộp {result} giá trị."));
    }

    // ---------------------------------------------------------------
    // Hàng đợi biên mục chi tiết (II.4)
    // ---------------------------------------------------------------

    /// <summary>Danh sách việc trong hàng đợi, lọc theo cột của bảng công việc.</summary>
    [HttpGet("queue")]
    [RequirePermission(PermissionCodes.CatalogQueueView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CatalogQueueItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<CatalogQueueItemDto>>>> GetQueue(
        [FromQuery] CatalogQueueRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCatalogQueueQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Số việc trong từng cột của bảng công việc.</summary>
    [HttpGet("queue/summary")]
    [RequirePermission(PermissionCodes.CatalogQueueView)]
    [ProducesResponseType(typeof(ApiResponse<CatalogQueueSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CatalogQueueSummaryDto>>> GetQueueSummary(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCatalogQueueSummaryQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Thống kê năng suất biên mục theo cán bộ.</summary>
    [HttpGet("queue/productivity")]
    [RequirePermission(PermissionCodes.CatalogQueueView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CatalogProductivityDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CatalogProductivityDto>>>> GetQueueProductivity(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCatalogProductivityQuery(from, to), ct);
        return Ok(Success(result));
    }

    /// <summary>Đưa một biểu ghi vào hàng đợi biên mục chi tiết.</summary>
    [HttpPost("queue")]
    [RequirePermission(PermissionCodes.CatalogQueueAssign)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<Guid>>> Enqueue(
        [FromBody] EnqueueForCatalogingCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã đưa biểu ghi vào hàng đợi biên mục."));
    }

    /// <summary>Phân công việc cho cán bộ, kèm độ ưu tiên và hạn xử lý.</summary>
    [HttpPost("queue/assign")]
    [RequirePermission(PermissionCodes.CatalogQueueAssign)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> AssignQueue(
        [FromBody] AssignCatalogQueueCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, command.AssignedTo is null
            ? $"Đã bỏ phân công {result} việc."
            : $"Đã phân công {result} việc."));
    }

    /// <summary>
    /// Chuyển trạng thái nhiều việc cùng lúc — dùng khi duyệt cả một lượt thu hoạch.
    /// </summary>
    [HttpPost("queue/status")]
    [RequirePermission(PermissionCodes.CatalogQueueProcess)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<int>>> ChangeQueueStatusBatch(
        [FromBody] ChangeCatalogQueueStatusBatchCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result, $"Đã cập nhật trạng thái {result} việc."));
    }

    /// <summary>Chuyển trạng thái một việc: nhận việc, gửi duyệt, duyệt xong hoặc trả lại.</summary>
    [HttpPost("queue/{id:guid}/status")]
    [RequirePermission(PermissionCodes.CatalogQueueProcess)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> ChangeQueueStatus(
        Guid id, [FromBody] ChangeCatalogQueueStatusCommand command, CancellationToken ct)
    {
        command.Id = id;
        await Mediator.Send(command, ct);

        return Ok(Success<object?>(null, "Đã cập nhật trạng thái công việc."));
    }

    /// <summary>Bỏ một việc khỏi hàng đợi; biểu ghi không bị ảnh hưởng.</summary>
    [HttpDelete("queue/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogQueueAssign)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> RemoveFromQueue(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new RemoveFromCatalogQueueCommand(id), ct);
        return Ok(Success<object?>(null, "Đã bỏ việc khỏi hàng đợi."));
    }

    // ---------------------------------------------------------------
    // Mẫu phích và in phích (II.10)
    // ---------------------------------------------------------------

    /// <summary>Danh sách mẫu phích đã thiết kế.</summary>
    [HttpGet("card-templates")]
    [RequirePermission(PermissionCodes.CatalogCardPrint)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CardTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CardTemplateDto>>>> GetCardTemplates(
        [FromQuery] bool includeInactive, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCardTemplatesQuery(includeInactive), ct);
        return Ok(Success(result));
    }

    [HttpPost("card-templates")]
    [RequirePermission(PermissionCodes.CatalogCardTemplateManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateCardTemplate(
        [FromBody] SaveCardTemplateCommand command, CancellationToken ct)
    {
        command.Id = null;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã thêm mẫu phích."));
    }

    [HttpPut("card-templates/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogCardTemplateManage)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateCardTemplate(
        Guid id, [FromBody] SaveCardTemplateCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã cập nhật mẫu phích."));
    }

    [HttpDelete("card-templates/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogCardTemplateManage)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCardTemplate(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteCardTemplateCommand(id), ct);
        return Ok(Success<object?>(null, "Đã xóa mẫu phích."));
    }

    /// <summary>
    /// In phích ra PDF: chọn biểu ghi theo danh sách hoặc theo bộ lọc, chọn loại phích và mẫu.
    /// </summary>
    [HttpPost("cards/print")]
    [RequirePermission(PermissionCodes.CatalogCardPrint)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PrintCards([FromBody] PrintCardsRequestDto request, CancellationToken ct)
    {
        var file = await Mediator.Send(new PrintCardsCommand(request), ct);

        return File(file.Content, file.ContentType, file.FileName);
    }

    // ---------------------------------------------------------------
    // Nhập biểu ghi từ Excel (II.8)
    // ---------------------------------------------------------------

    /// <summary>Tải tệp Excel mẫu có tiêu đề tiếng Việt và sheet hướng dẫn từng cột.</summary>
    [HttpGet("excel/template")]
    [RequirePermission(PermissionCodes.CatalogBibImport)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExcelTemplate(CancellationToken ct)
    {
        var file = await Mediator.Send(new GetBibExcelTemplateQuery(), ct);

        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>
    /// Đọc thử tệp Excel: trả về danh sách cột, vài dòng đầu và ánh xạ hệ thống đoán được từ tên cột.
    /// </summary>
    [HttpPost("excel/preview")]
    [RequirePermission(PermissionCodes.CatalogBibImport)]
    [ProducesResponseType(typeof(ApiResponse<ExcelPreviewDto>), StatusCodes.Status200OK)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ExcelPreviewDto>>> PreviewExcel(
        IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Vui lòng chọn tệp Excel cần nhập."));
        }

        var content = await ReadAllAsync(file, ct);
        var result = await Mediator.Send(new PreviewBibExcelCommand(content), ct);

        return Ok(Success(result, $"Đọc được {result.TotalRows} dòng dữ liệu."));
    }

    /// <summary>Bắt đầu nhập từ Excel. Tác vụ chạy nền, theo dõi tiến độ qua mã trả về.</summary>
    [HttpPost("excel/import")]
    [RequirePermission(PermissionCodes.CatalogBibImport)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<Guid>>> StartExcelImport(
        IFormFile file, [FromForm] string options, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Vui lòng chọn tệp Excel cần nhập."));
        }

        ExcelImportOptions parsed;

        try
        {
            parsed = System.Text.Json.JsonSerializer.Deserialize<ExcelImportOptions>(
                         string.IsNullOrWhiteSpace(options) ? "{}" : options,
                         ImportOptionsJson)
                     ?? new ExcelImportOptions();
        }
        catch (System.Text.Json.JsonException exception)
        {
            return BadRequest(ApiResponse.Fail($"Tùy chọn nhập dữ liệu không đọc được: {exception.Message}"));
        }

        var content = await ReadAllAsync(file, ct);
        var jobId = await Mediator.Send(new StartBibExcelImportCommand(content, file.FileName, parsed), ct);

        return Ok(Success(jobId, "Đã bắt đầu nhập dữ liệu từ bảng tính."));
    }

    /// <summary>Các hồ sơ ánh xạ cột đã lưu, để dùng lại cho tệp cùng khuôn.</summary>
    [HttpGet("excel/mapping-profiles")]
    [RequirePermission(PermissionCodes.CatalogBibImport)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ImportMappingProfileDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ImportMappingProfileDto>>>> GetMappingProfiles(
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetImportMappingProfilesQuery(), ct);
        return Ok(Success(result));
    }

    [HttpPost("excel/mapping-profiles")]
    [RequirePermission(PermissionCodes.CatalogBibImport)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateMappingProfile(
        [FromBody] SaveImportMappingProfileCommand command, CancellationToken ct)
    {
        command.Id = null;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu hồ sơ ánh xạ."));
    }

    [HttpPut("excel/mapping-profiles/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogBibImport)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> UpdateMappingProfile(
        Guid id, [FromBody] SaveImportMappingProfileCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã cập nhật hồ sơ ánh xạ."));
    }

    [HttpDelete("excel/mapping-profiles/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogBibImport)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteMappingProfile(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteImportMappingProfileCommand(id), ct);
        return Ok(Success<object?>(null, "Đã xóa hồ sơ ánh xạ."));
    }

    /// <summary>
    /// Tùy chọn nhập đi kèm tệp trong một biểu mẫu multipart nên phải tự đọc từ chuỗi JSON.
    /// Cấu hình phải khớp với cấu hình chung của API, nhất là việc đọc enum theo tên.
    /// </summary>
    private static readonly System.Text.Json.JsonSerializerOptions ImportOptionsJson = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private static async Task<byte[]> ReadAllAsync(IFormFile file, CancellationToken ct)
    {
        using var buffer = new MemoryStream();

        await using (var stream = file.OpenReadStream())
        {
            await stream.CopyToAsync(buffer, ct);
        }

        return buffer.ToArray();
    }
}

/// <summary>Yêu cầu xuất biểu ghi: hoặc danh sách đã chọn, hoặc bộ lọc đang dùng.</summary>
public class ExportBibsRequest
{
    /// <summary>Danh sách biểu ghi đã tick chọn. Bỏ trống thì xuất theo bộ lọc.</summary>
    public List<Guid> Ids { get; set; } = new();

    public BibListRequest? Filter { get; set; }

    /// <summary>iso2709 hoặc marcxml.</summary>
    public string Format { get; set; } = "iso2709";
}

/// <summary>Yêu cầu gộp giá trị của danh mục tự tạo.</summary>
public class MergeCustomIndexRequest
{
    /// <summary>Giá trị được giữ lại.</summary>
    public Guid KeepId { get; set; }

    /// <summary>Các giá trị bị gộp vào giá trị giữ lại.</summary>
    public List<Guid> MergeIds { get; set; } = new();
}

/// <summary>Lý do xóa, bắt buộc nhập khi xóa biểu ghi hoặc đăng ký cá biệt.</summary>
public class DeleteBibRequest
{
    public string Reason { get; set; } = string.Empty;
}
