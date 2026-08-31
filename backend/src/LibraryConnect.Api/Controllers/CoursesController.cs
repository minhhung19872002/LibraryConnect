using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Courses;
using LibraryConnect.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Phân hệ X — Tài liệu môn học: gán môn học vào ngành đào tạo, liên kết tài liệu với môn học và
/// ba báo cáo đi kèm.
///
/// Danh mục ngành và môn học dùng chung màn hình Danh mục của hệ thống; ở đây là những việc riêng
/// của phân hệ này.
/// </summary>
[Route("api/courses")]
[Tags("Tài liệu môn học")]
public class CoursesController : ApiControllerBase
{
    /// <summary>Danh sách môn học kèm ngành đào tạo và số tài liệu đã gán.</summary>
    [HttpGet]
    [RequirePermission(PermissionCodes.CourseManage)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CourseRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<CourseRowDto>>>> Courses(
        [FromQuery] CourseListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCoursesQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Gán một môn học vào các ngành đào tạo dạy môn đó.</summary>
    [HttpPut("{id:guid}/majors")]
    [RequirePermission(PermissionCodes.CourseManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> SetMajors(
        Guid id, [FromBody] SetCourseMajorsBody body, CancellationToken ct)
    {
        await Mediator.Send(new SetCourseMajorsCommand(id, body.MajorIds ?? new List<Guid>()), ct);
        return Ok(SuccessMessage("Đã lưu danh sách ngành của môn học."));
    }

    /// <summary>Danh sách ngành đào tạo dạy một môn học.</summary>
    public class SetCourseMajorsBody
    {
        public List<Guid>? MajorIds { get; set; }
    }

    // ---------------------------------------------------------------
    // X.3 — Liên kết tài liệu theo môn học
    // ---------------------------------------------------------------

    /// <summary>Tài liệu đã gán cho một môn học.</summary>
    [HttpGet("{id:guid}/documents")]
    [RequirePermission(PermissionCodes.CourseDocumentLink)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CourseDocumentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CourseDocumentDto>>>> Documents(
        Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCourseDocumentsQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Gán một hoặc nhiều tài liệu cho môn học.</summary>
    [HttpPost("{id:guid}/documents")]
    [RequirePermission(PermissionCodes.CourseDocumentLink)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> AssignDocuments(
        Guid id, [FromBody] AssignCourseDocumentsCommand command, CancellationToken ct)
    {
        command.CourseId = id;
        var added = await Mediator.Send(command, ct);

        return Ok(Success(added, added == command.BibIds.Distinct().Count()
            ? $"Đã gán {added} tài liệu."
            : $"Đã gán {added} tài liệu mới, các tài liệu còn lại được cập nhật mức độ."));
    }

    /// <summary>Đổi mức độ liên quan hoặc ghi chú của một liên kết.</summary>
    [HttpPut("documents/{linkId:guid}")]
    [RequirePermission(PermissionCodes.CourseDocumentLink)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> UpdateDocument(
        Guid linkId, [FromBody] UpdateCourseDocumentBody body, CancellationToken ct)
    {
        await Mediator.Send(new UpdateCourseDocumentCommand(linkId, body.RelationType, body.Note), ct);
        return Ok(SuccessMessage("Đã cập nhật liên kết."));
    }

    /// <summary>Mức độ liên quan và ghi chú của một liên kết tài liệu môn học.</summary>
    public class UpdateCourseDocumentBody
    {
        public CourseRelationType RelationType { get; set; } = CourseRelationType.RequiredReference;
        public string? Note { get; set; }
    }

    /// <summary>Bỏ một tài liệu khỏi môn học.</summary>
    [HttpDelete("documents/{linkId:guid}")]
    [RequirePermission(PermissionCodes.CourseDocumentLink)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> RemoveDocument(Guid linkId, CancellationToken ct)
    {
        await Mediator.Send(new RemoveCourseDocumentCommand(linkId), ct);
        return Ok(SuccessMessage("Đã bỏ tài liệu khỏi môn học."));
    }

    // ---------------------------------------------------------------
    // Nhập từ Excel
    // ---------------------------------------------------------------

    /// <summary>Tệp Excel mẫu để nhập danh mục tài liệu môn học.</summary>
    [HttpGet("documents/import/template")]
    [RequirePermission(PermissionCodes.CourseDocumentLink)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportTemplate(CancellationToken ct)
    {
        var content = await Mediator.Send(new GetCourseDocumentTemplateQuery(), ct);

        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "mau-tai-lieu-mon-hoc.xlsx");
    }

    /// <summary>
    /// Nhập danh mục tài liệu môn học từ Excel.
    /// </summary>
    /// <remarks>
    /// Đặt <c>dryRun=true</c> để chỉ kiểm tra tệp và nhận về danh sách dòng lỗi mà chưa ghi gì —
    /// đây là bước cán bộ luôn làm trước với tệp do khoa gửi sang.
    /// </remarks>
    [HttpPost("documents/import")]
    [RequirePermission(PermissionCodes.CourseDocumentLink)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<CourseDocumentImportResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CourseDocumentImportResultDto>>> Import(
        IFormFile file, [FromQuery] bool dryRun, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Chưa chọn tệp Excel."));
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);

        var result = await Mediator.Send(
            new ImportCourseDocumentsCommand(buffer.ToArray(), dryRun), ct);

        return Ok(Success(result, dryRun
            ? $"Đã kiểm tra {result.TotalRows} dòng, {result.FailedRows} dòng có lỗi."
            : $"Đã nhập {result.SuccessRows} dòng, {result.FailedRows} dòng có lỗi."));
    }

    // ---------------------------------------------------------------
    // Báo cáo
    // ---------------------------------------------------------------

    /// <summary>Ba báo cáo: môn chưa có tài liệu, tài liệu dùng chung nhiều môn, đáp ứng theo ngành.</summary>
    [HttpGet("reports")]
    [RequirePermission(PermissionCodes.CourseReportView)]
    [ProducesResponseType(typeof(ApiResponse<CourseReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CourseReportDto>>> Reports(
        [FromQuery] Guid? majorId, [FromQuery] int top, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCourseReportQuery(majorId, top <= 0 ? 20 : top), ct);
        return Ok(Success(result));
    }

    /// <summary>Xuất báo cáo ra Excel hoặc PDF.</summary>
    [HttpGet("reports/export")]
    [RequirePermission(PermissionCodes.CourseReportView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportReport(
        [FromQuery] string format, [FromQuery] Guid? majorId, CancellationToken ct)
    {
        var file = await Mediator.Send(new ExportCourseReportQuery(format ?? "excel", majorId), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
