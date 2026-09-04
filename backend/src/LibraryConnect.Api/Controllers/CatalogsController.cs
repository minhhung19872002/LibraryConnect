using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Catalogs;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Danh mục nghiệp vụ (mục 4.2 và II.9).
///
/// Một bộ endpoint dùng chung cho mọi danh mục: đoạn <c>{catalog}</c> trên đường dẫn là mã danh mục
/// lấy từ <c>GET /api/catalogs</c>, ví dụ <c>document-types</c>, <c>authors</c>, <c>classifications</c>.
/// </summary>
[Route("api/catalogs")]
[Tags("Danh mục")]
public class CatalogsController : ApiControllerBase
{
    /// <summary>Danh sách các danh mục hiện có kèm mô tả cấu trúc của từng danh mục.</summary>
    [HttpGet]
    [RequirePermission(PermissionCodes.CatalogListView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CatalogMetadataDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CatalogMetadataDto>>>> GetCatalogs(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCatalogListQuery(), ct);
        return Ok(Success(result));
    }

    /// <summary>Mô tả một danh mục: các trường riêng, có phân cấp không, có hỗ trợ gộp trùng không.</summary>
    [HttpGet("{catalog}/metadata")]
    [RequirePermission(PermissionCodes.CatalogListView)]
    [ProducesResponseType(typeof(ApiResponse<CatalogMetadataDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CatalogMetadataDto>>> GetMetadata(string catalog, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCatalogMetadataQuery(catalog), ct);
        return Ok(Success(result));
    }

    /// <summary>Danh sách giá trị của một danh mục.</summary>
    [HttpGet("{catalog}/items")]
    [RequirePermission(PermissionCodes.CatalogListView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CatalogItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<CatalogItemDto>>>> GetItems(
        string catalog, [FromQuery] CatalogListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCatalogItemsQuery(catalog, request), ct);
        return Ok(Success(result));
    }

    /// <summary>Toàn bộ cây của một danh mục phân cấp, dùng cho ô chọn cấp trên và bộ lọc dạng cây.</summary>
    [HttpGet("{catalog}/tree")]
    [RequirePermission(PermissionCodes.CatalogListView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CatalogTreeNodeDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CatalogTreeNodeDto>>>> GetTree(
        string catalog, [FromQuery] bool activeOnly, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCatalogTreeQuery(catalog, activeOnly), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một giá trị danh mục.</summary>
    [HttpGet("{catalog}/items/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogListView)]
    [ProducesResponseType(typeof(ApiResponse<CatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CatalogItemDto>>> GetItem(string catalog, Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCatalogItemQuery(catalog, id), ct);
        return Ok(Success(result));
    }

    /// <summary>Thêm một giá trị. Bỏ trống mã thì hệ thống tự sinh từ tên.</summary>
    [HttpPost("{catalog}/items")]
    [RequirePermission(PermissionCodes.CatalogListCreate)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        string catalog, [FromBody] CatalogItemInput input, CancellationToken ct)
    {
        var id = await Mediator.Send(new CreateCatalogItemCommand(catalog, input), ct);
        return Ok(Success(id, "Thêm giá trị danh mục thành công."));
    }

    /// <summary>Sửa một giá trị danh mục.</summary>
    [HttpPut("{catalog}/items/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogListUpdate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Update(
        string catalog, Guid id, [FromBody] CatalogItemInput input, CancellationToken ct)
    {
        await Mediator.Send(new UpdateCatalogItemCommand(catalog, id, input), ct);
        return Ok(SuccessMessage("Cập nhật giá trị danh mục thành công."));
    }

    /// <summary>
    /// Xóa một giá trị. Bị từ chối nếu giá trị đang được bản ghi nghiệp vụ sử dụng hoặc còn giá trị con.
    /// </summary>
    [HttpDelete("{catalog}/items/{id:guid}")]
    [RequirePermission(PermissionCodes.CatalogListDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> Delete(string catalog, Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteCatalogItemCommand(catalog, id), ct);
        return Ok(SuccessMessage("Xóa giá trị danh mục thành công."));
    }

    /// <summary>Tìm các giá trị nghi trùng, so sánh theo tên đã bỏ dấu và bỏ phân biệt hoa thường.</summary>
    [HttpGet("{catalog}/duplicates")]
    [RequirePermission(PermissionCodes.CatalogListMerge)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DuplicateGroupDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DuplicateGroupDto>>>> GetDuplicates(
        string catalog, CancellationToken ct)
    {
        var result = await Mediator.Send(new FindDuplicateCatalogItemsQuery(catalog), ct);
        return Ok(Success(result));
    }

    /// <summary>
    /// Gộp các giá trị trùng vào một giá trị giữ lại. Mọi tham chiếu từ biểu ghi được chuyển sang
    /// giá trị giữ lại trước khi các giá trị trùng bị xóa.
    /// </summary>
    [HttpPost("{catalog}/merge")]
    [RequirePermission(PermissionCodes.CatalogListMerge)]
    [ProducesResponseType(typeof(ApiResponse<CatalogMergeResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CatalogMergeResultDto>>> Merge(
        string catalog, [FromBody] MergeCatalogRequest body, CancellationToken ct)
    {
        var result = await Mediator.Send(new MergeCatalogItemsCommand(catalog, body.TargetId, body.SourceIds), ct);

        return Ok(Success(result,
            $"Đã gộp {result.MergedCount} giá trị vào '{result.TargetName}' và chuyển {result.UpdatedReferences} tham chiếu."));
    }

    /// <summary>Tải tệp Excel mẫu để nhập danh mục.</summary>
    [HttpGet("{catalog}/import-template")]
    [RequirePermission(PermissionCodes.CatalogListImport)]
    public async Task<IActionResult> GetImportTemplate(string catalog, CancellationToken ct)
    {
        var file = await Mediator.Send(new GetCatalogTemplateQuery(catalog), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>
    /// Xuất toàn bộ danh mục. Excel dùng đúng tiêu đề cột của tệp mẫu nên sửa xong nhập lại được;
    /// <c>format=Pdf</c> cho bản in ra giấy.
    /// </summary>
    [HttpGet("{catalog}/export")]
    [RequirePermission(PermissionCodes.CatalogListExport)]
    public async Task<IActionResult> Export(
        string catalog, [FromQuery] Application.Features.Admin.AuditLogs.ExportFormat format, CancellationToken ct)
    {
        var file = await Mediator.Send(new ExportCatalogQuery(catalog, format), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>
    /// Nhập danh mục từ Excel. Dòng có mã đã tồn tại sẽ cập nhật giá trị hiện có.
    /// Đặt <c>dryRun=true</c> để kiểm tra tệp mà không ghi bản ghi nào.
    /// </summary>
    [HttpPost("{catalog}/import")]
    [RequirePermission(PermissionCodes.CatalogListImport)]
    [ProducesResponseType(typeof(ApiResponse<CatalogImportResultDto>), StatusCodes.Status200OK)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<CatalogImportResultDto>>> Import(
        string catalog, IFormFile file, [FromQuery] bool dryRun, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Vui lòng chọn tệp Excel cần nhập."));
        }

        await using var stream = file.OpenReadStream();
        var result = await Mediator.Send(new ImportCatalogCommand(catalog, stream, file.FileName, dryRun), ct);

        var message = dryRun
            ? $"Kiểm tra xong: thêm mới {result.CreatedRows}, cập nhật {result.UpdatedRows}, lỗi {result.ErrorRows}."
            : $"Nhập xong: thêm mới {result.CreatedRows}, cập nhật {result.UpdatedRows}, lỗi {result.ErrorRows}.";

        return Ok(Success(result, message));
    }
}

public class MergeCatalogRequest
{
    /// <summary>Giá trị được giữ lại sau khi gộp.</summary>
    public Guid TargetId { get; set; }

    /// <summary>Các giá trị trùng sẽ bị gộp vào giá trị giữ lại.</summary>
    public List<Guid> SourceIds { get; set; } = new();
}
