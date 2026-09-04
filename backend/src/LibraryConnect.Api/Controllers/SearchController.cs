using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Opac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Tra cứu công khai (Phân hệ IX) — cũng chính là nhóm /api/search, /api/browse và /api/bib mà ứng
/// dụng di động đợt sau gọi (mục XI.4).
///
/// Không cần đăng nhập: tra cứu là dịch vụ công của thư viện. Nhưng có giới hạn tần suất, vì đây là
/// nhóm endpoint mở ra Internet.
/// </summary>
[Route("api")]
[AllowAnonymous]
[EnableRateLimiting("public")]
[Tags("Tra cứu (OPAC / ứng dụng khách)")]
public class SearchController : ApiControllerBase
{
    // ---------------------------------------------------------------
    // Tìm kiếm
    // ---------------------------------------------------------------

    /// <summary>Tìm kiếm cơ bản: từ khóa, phạm vi, bộ lọc, sắp xếp và phân trang.</summary>
    /// <remarks>
    /// Gõ không dấu vẫn ra kết quả có dấu: "co so du lieu" tìm được "Cơ sở dữ liệu".
    /// </remarks>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OpacResultDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<OpacResultDto>>>> Search(
        [FromQuery] OpacSearchRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new OpacSearchQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Tìm kiếm nâng cao: nhiều điều kiện nối bằng VÀ / HOẶC / KHÔNG.</summary>
    [HttpPost("search/advanced")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OpacResultDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<OpacResultDto>>>> AdvancedSearch(
        [FromBody] OpacAdvancedSearchRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new OpacAdvancedSearchQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Gợi ý tự động khi gõ vào ô tìm kiếm.</summary>
    [HttpGet("search/suggest")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OpacSuggestionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OpacSuggestionDto>>>> Suggest(
        [FromQuery] string term, [FromQuery] int limit, CancellationToken ct)
    {
        var result = await Mediator.Send(new OpacSuggestQuery(term, limit <= 0 ? 10 : limit), ct);
        return Ok(Success(result));
    }

    /// <summary>Bộ đếm cho các bộ lọc bên trái kết quả, tính trên đúng tập kết quả hiện tại.</summary>
    [HttpGet("search/facets")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OpacFacetGroupDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OpacFacetGroupDto>>>> Facets(
        [FromQuery] OpacSearchRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new OpacFacetsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Tra theo ISBN hoặc ISSN — dùng khi quét mã vạch trên bìa sách.</summary>
    [HttpGet("search/by-isbn/{isbn}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OpacResultDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OpacResultDto>>>> ByIsbn(
        string isbn, CancellationToken ct)
    {
        var result = await Mediator.Send(new OpacSearchByIsbnQuery(isbn), ct);
        return Ok(Success(result));
    }

    /// <summary>Tra một bản in theo mã vạch ĐKCB — quét mã trên gáy sách.</summary>
    [HttpGet("search/by-barcode/{barcode}")]
    [ProducesResponseType(typeof(ApiResponse<OpacBarcodeResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OpacBarcodeResultDto>>> ByBarcode(
        string barcode, CancellationToken ct)
    {
        var result = await Mediator.Send(new OpacSearchByBarcodeQuery(barcode), ct);
        return Ok(Success(result));
    }

    // ---------------------------------------------------------------
    // Chi tiết tài liệu
    // ---------------------------------------------------------------

    /// <summary>Chi tiết một tài liệu: mô tả thư mục, danh sách bản in kèm vị trí và tình trạng.</summary>
    [HttpGet("bib/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OpacBibDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OpacBibDetailDto>>> Bib(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOpacBibQuery(id), ct);

        // Đếm lượt xem sau khi đã lấy được dữ liệu: tài liệu không tồn tại thì không cộng lượt.
        await Mediator.Send(new RecordOpacBibViewCommand(id), ct);

        return Ok(Success(result));
    }

    /// <summary>Trích dẫn tài liệu theo chuẩn APA, MLA, Chicago, BibTeX, RIS hoặc EndNote.</summary>
    [HttpGet("bib/{id:guid}/citation")]
    [ProducesResponseType(typeof(ApiResponse<CitationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CitationDto>>> Citation(
        Guid id, [FromQuery] CitationStyle style, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCitationQuery(id, style), ct);
        return Ok(Success(result));
    }

    /// <summary>Tải tệp trích dẫn để nạp vào phần mềm quản lý tài liệu tham khảo.</summary>
    [HttpGet("bib/{id:guid}/citation/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadCitation(
        Guid id, [FromQuery] CitationStyle style, CancellationToken ct)
    {
        var citation = await Mediator.Send(new GetCitationQuery(id, style), ct);
        var bytes = System.Text.Encoding.UTF8.GetBytes(citation.Content);

        return File(bytes, citation.ContentType, citation.FileName ?? "trich-dan.txt");
    }

    // ---------------------------------------------------------------
    // Duyệt theo danh mục
    // ---------------------------------------------------------------

    /// <summary>Duyệt theo chủ đề. Bỏ trống mã cha để lấy cấp trên cùng.</summary>
    [HttpGet("browse/subjects")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OpacBrowseEntryDto>>), StatusCodes.Status200OK)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<OpacBrowseEntryDto>>>> BrowseSubjects(
        [FromQuery] Guid? parentId, [FromQuery] string? letter, CancellationToken ct) =>
        BrowseAsync(OpacBrowseKind.Subject, parentId, letter, ct);

    /// <summary>Duyệt theo tác giả, lọc được theo chữ cái đầu (mọi nhánh duyệt đều nhận tham số này).</summary>
    [HttpGet("browse/authors")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OpacBrowseEntryDto>>), StatusCodes.Status200OK)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<OpacBrowseEntryDto>>>> BrowseAuthors(
        [FromQuery] string? letter, CancellationToken ct) =>
        BrowseAsync(OpacBrowseKind.Author, null, letter, ct);

    /// <summary>Duyệt theo khung phân loại.</summary>
    [HttpGet("browse/classifications")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OpacBrowseEntryDto>>), StatusCodes.Status200OK)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<OpacBrowseEntryDto>>>> BrowseClassifications(
        [FromQuery] Guid? parentId, [FromQuery] string? letter, CancellationToken ct) =>
        BrowseAsync(OpacBrowseKind.Classification, parentId, letter, ct);

    /// <summary>Duyệt theo bộ sưu tập.</summary>
    [HttpGet("browse/collections")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OpacBrowseEntryDto>>), StatusCodes.Status200OK)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<OpacBrowseEntryDto>>>> BrowseCollections(
        [FromQuery] string? letter, CancellationToken ct) =>
        BrowseAsync(OpacBrowseKind.Collection, null, letter, ct);

    /// <summary>Duyệt theo ngành đào tạo.</summary>
    [HttpGet("browse/majors")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OpacBrowseEntryDto>>), StatusCodes.Status200OK)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<OpacBrowseEntryDto>>>> BrowseMajors(
        [FromQuery] string? letter, CancellationToken ct) =>
        BrowseAsync(OpacBrowseKind.Major, null, letter, ct);

    /// <summary>Duyệt theo môn học; truyền mã ngành để chỉ lấy môn của ngành đó.</summary>
    [HttpGet("browse/courses")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OpacBrowseEntryDto>>), StatusCodes.Status200OK)]
    public Task<ActionResult<ApiResponse<IReadOnlyList<OpacBrowseEntryDto>>>> BrowseCourses(
        [FromQuery] Guid? majorId, [FromQuery] string? letter, CancellationToken ct) =>
        BrowseAsync(OpacBrowseKind.Course, majorId, letter, ct);

    /// <summary>Tài liệu của một môn học, chia theo giáo trình chính và tài liệu tham khảo.</summary>
    [HttpGet("browse/majors/{majorId:guid}/courses/{courseId:guid}/documents")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OpacCourseDocumentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<OpacCourseDocumentDto>>>> CourseDocuments(
        Guid majorId, Guid courseId, [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        // Mã ngành nằm trong đường dẫn để địa chỉ đọc lên là hiểu ngay đang xem môn của ngành nào;
        // việc lọc thì chỉ cần mã môn, vì một môn chỉ có một danh mục tài liệu.
        _ = majorId;

        var result = await Mediator.Send(new OpacCourseDocumentsQuery(courseId, request), ct);
        return Ok(Success(result));
    }

    /// <summary>Danh mục luận văn – luận án.</summary>
    [HttpGet("browse/theses")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OpacResultDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<OpacResultDto>>>> Theses(
        [FromQuery] OpacSearchRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new OpacThesesQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Danh mục ấn phẩm định kỳ kèm tình trạng nhận số.</summary>
    [HttpGet("browse/serials")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OpacSerialDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<OpacSerialDto>>>> Serials(
        [FromQuery] PagedRequestDefault request, CancellationToken ct)
    {
        var result = await Mediator.Send(new OpacSerialsQuery(request), ct);
        return Ok(Success(result));
    }

    private async Task<ActionResult<ApiResponse<IReadOnlyList<OpacBrowseEntryDto>>>> BrowseAsync(
        OpacBrowseKind kind, Guid? parentId, string? letter, CancellationToken ct)
    {
        var result = await Mediator.Send(new OpacBrowseQuery(kind, parentId, letter), ct);
        return Ok(Success(result));
    }
}
