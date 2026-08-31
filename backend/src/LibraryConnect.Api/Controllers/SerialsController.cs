using LibraryConnect.Api.Security;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Common.Security;
using LibraryConnect.Application.Features.Admin.AuditLogs;
using LibraryConnect.Application.Features.Serials;
using LibraryConnect.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Phân hệ IV — Ấn phẩm định kỳ: đầu báo và tạp chí, sinh số dự kiến, ghi nhận số đến, khiếu nại
/// số thiếu, mục lục bài trích, đóng tập và báo cáo.
/// </summary>
[Route("api/serials")]
[Tags("Ấn phẩm định kỳ")]
public class SerialsController : ApiControllerBase
{
    /// <summary>Tìm kiếm báo, tạp chí theo tên, ISSN, kỳ hạn, ngôn ngữ, kho và trạng thái đặt.</summary>
    [HttpGet]
    [RequirePermission(PermissionCodes.SerialView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SerialDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<SerialDto>>>> Search(
        [FromQuery] SerialListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchSerialsQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Chi tiết một đầu báo kèm khai báo kỳ hạn.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCodes.SerialView)]
    [ProducesResponseType(typeof(ApiResponse<SerialDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SerialDetailDto>>> Get(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetSerialQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Khai báo một đầu báo mới. Hệ thống tạo kèm biểu ghi MARC 21 của ấn phẩm.</summary>
    [HttpPost]
    [RequirePermission(PermissionCodes.SerialCreate)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] SaveSerialCommand command, CancellationToken ct)
    {
        command.Id = null;
        var id = await Mediator.Send(command, ct);
        return Ok(Success(id, "Đã lưu đầu báo."));
    }

    /// <summary>Sửa đầu báo: kỳ hạn, phân kho, thời gian đặt mua.</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionCodes.SerialUpdate)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Guid>>> Update(
        Guid id, [FromBody] SaveSerialCommand command, CancellationToken ct)
    {
        command.Id = id;
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, "Đã lưu đầu báo."));
    }

    /// <summary>Xóa đầu báo khi chưa nhận số nào.</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionCodes.SerialDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteSerialCommand(id), ct);
        return Ok(SuccessMessage("Đã xóa đầu báo."));
    }

    // ---------------------------------------------------------------
    // Sinh số và ghi nhận
    // ---------------------------------------------------------------

    /// <summary>Xem trước danh sách số sẽ sinh, chưa ghi vào cơ sở dữ liệu.</summary>
    [HttpGet("{id:guid}/issues/preview")]
    [RequirePermission(PermissionCodes.SerialPredict)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<IssuePreviewDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IssuePreviewDto>>>> PreviewIssues(
        Guid id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var result = await Mediator.Send(new PreviewSerialIssuesQuery(id, from, to), ct);
        return Ok(Success(result));
    }

    /// <summary>Chốt danh sách số dự kiến. Nhận nhiều đầu báo cùng lúc cho màn hình bổ sung tổng thể.</summary>
    [HttpPost("issues/generate")]
    [RequirePermission(PermissionCodes.SerialPredict)]
    [ProducesResponseType(typeof(ApiResponse<GenerateIssuesResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<GenerateIssuesResultDto>>> GenerateIssues(
        [FromBody] GenerateSerialIssuesCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result,
            result.Skipped > 0
                ? $"Đã sinh {result.Created} số, bỏ qua {result.Skipped} số đã có."
                : $"Đã sinh {result.Created} số dự kiến."));
    }

    /// <summary>Danh sách số của một hoặc nhiều đầu báo.</summary>
    [HttpGet("issues")]
    [RequirePermission(PermissionCodes.SerialView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SerialIssueDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<SerialIssueDto>>>> SearchIssues(
        [FromQuery] SerialIssueListRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchSerialIssuesQuery(request), ct);
        return Ok(Success(result));
    }

    /// <summary>Lưới tình trạng nhận số theo năm.</summary>
    [HttpGet("{id:guid}/grid")]
    [RequirePermission(PermissionCodes.SerialView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<IssueGridYearDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IssueGridYearDto>>>> Grid(
        Guid id, [FromQuery] int? fromYear, [FromQuery] int? toYear, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetIssueGridQuery(id, fromYear, toYear), ct);
        return Ok(Success(result));
    }

    /// <summary>Bảng tổng hợp tình hình nhận số của một đầu báo theo năm.</summary>
    [HttpGet("{id:guid}/summary")]
    [RequirePermission(PermissionCodes.SerialView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SerialSummaryRowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SerialSummaryRowDto>>>> Summary(
        Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetSerialSummaryQuery(id), ct);
        return Ok(Success(result));
    }

    /// <summary>Ghi nhận số đến, một số hoặc cả loạt; sinh ĐKCB và mã vạch cho từng bản.</summary>
    [HttpPost("issues/receive")]
    [RequirePermission(PermissionCodes.SerialReceive)]
    [ProducesResponseType(typeof(ApiResponse<ReceiveIssuesResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ReceiveIssuesResultDto>>> ReceiveIssues(
        [FromBody] ReceiveSerialIssuesCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result,
            $"Đã ghi nhận {result.Received} số và tạo {result.CreatedItems} ĐKCB."));
    }

    /// <summary>Đánh dấu các số quá hạn là thiếu.</summary>
    [HttpPost("issues/mark-missing")]
    [RequirePermission(PermissionCodes.SerialReceive)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<int>>> MarkMissing(
        [FromBody] MarkIssuesMissingCommand command, CancellationToken ct)
    {
        var affected = await Mediator.Send(command, ct);
        return Ok(Success(affected, $"Đã đánh dấu {affected} số là thiếu."));
    }

    // ---------------------------------------------------------------
    // Khiếu nại
    // ---------------------------------------------------------------

    /// <summary>Danh sách phiếu khiếu nại nhà cung cấp.</summary>
    [HttpGet("claims")]
    [RequirePermission(PermissionCodes.SerialClaim)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SerialClaimDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SerialClaimDto>>>> Claims(
        [FromQuery] Guid? serialId, [FromQuery] SerialClaimStatus? status, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetSerialClaimsQuery(serialId, status), ct);
        return Ok(Success(result));
    }

    /// <summary>Lập phiếu khiếu nại cho các số thiếu.</summary>
    [HttpPost("claims")]
    [RequirePermission(PermissionCodes.SerialClaim)]
    [ProducesResponseType(typeof(ApiResponse<CreateClaimsResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CreateClaimsResultDto>>> CreateClaims(
        [FromBody] CreateSerialClaimsCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(Success(result, $"Đã lập {result.Created} phiếu khiếu nại."));
    }

    /// <summary>Ghi nhận phản hồi của nhà cung cấp.</summary>
    [HttpPost("claims/{id:guid}/respond")]
    [RequirePermission(PermissionCodes.SerialClaim)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> RespondClaim(
        Guid id, [FromBody] RespondClaimBody body, CancellationToken ct)
    {
        await Mediator.Send(new RespondSerialClaimCommand(id, body.Response, body.Status), ct);
        return Ok(SuccessMessage("Đã ghi nhận phản hồi."));
    }

    // ---------------------------------------------------------------
    // Bài trích (IV.2)
    // ---------------------------------------------------------------

    /// <summary>Mục lục bài trích của một số.</summary>
    [HttpGet("issues/{issueId:guid}/articles")]
    [RequirePermission(PermissionCodes.SerialView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SerialArticleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SerialArticleDto>>>> Articles(
        Guid issueId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetSerialArticlesQuery(issueId), ct);
        return Ok(Success(result));
    }

    /// <summary>Lưu toàn bộ mục lục bài trích của một số.</summary>
    [HttpPut("issues/{issueId:guid}/articles")]
    [RequirePermission(PermissionCodes.SerialArticleManage)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<int>>> SaveArticles(
        Guid issueId, [FromBody] SaveSerialArticlesCommand command, CancellationToken ct)
    {
        command.IssueId = issueId;
        var count = await Mediator.Send(command, ct);
        return Ok(Success(count, $"Đã lưu mục lục {count} bài."));
    }

    /// <summary>Sinh biểu ghi MARC riêng cho bài trích, liên kết về ấn phẩm mẹ qua trường 773.</summary>
    [HttpPost("issues/{issueId:guid}/articles/generate-records")]
    [RequirePermission(PermissionCodes.SerialArticleManage)]
    [ProducesResponseType(typeof(ApiResponse<GenerateArticleRecordsResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<GenerateArticleRecordsResultDto>>> GenerateArticleRecords(
        Guid issueId, [FromBody] GenerateArticleRecordsCommand command, CancellationToken ct)
    {
        command.IssueId = issueId;
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result,
            $"Đã sinh {result.Created} biểu ghi bài trích; bạn đọc tra được từ OPAC."));
    }

    /// <summary>Tải tệp Excel mẫu để nhập mục lục bài trích.</summary>
    [HttpGet("articles/excel-template")]
    [RequirePermission(PermissionCodes.SerialArticleManage)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ArticleTemplate(CancellationToken ct)
    {
        var file = await Mediator.Send(new GetArticleExcelTemplateQuery(), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Nhập mục lục bài trích của một số từ bảng tính.</summary>
    [HttpPost("issues/{issueId:guid}/articles/import")]
    [RequirePermission(PermissionCodes.SerialArticleManage)]
    [ProducesResponseType(typeof(ApiResponse<ImportArticlesResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ImportArticlesResultDto>>> ImportArticles(
        Guid issueId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Fail("Chưa chọn tệp Excel."));
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);

        var result = await Mediator.Send(
            new ImportSerialArticlesCommand(issueId, buffer.ToArray()), ct);

        return Ok(Success(result, $"Đã nhập {result.Imported} bài trích."));
    }

    // ---------------------------------------------------------------
    // Đóng tập (IV.4)
    // ---------------------------------------------------------------

    /// <summary>Danh sách tập đã đóng.</summary>
    [HttpGet("bindings")]
    [RequirePermission(PermissionCodes.SerialView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SerialBindingDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SerialBindingDto>>>> Bindings(
        [FromQuery] Guid? serialId, [FromQuery] int? year, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetSerialBindingsQuery(serialId, year), ct);
        return Ok(Success(result));
    }

    /// <summary>Đóng một khoảng số thành tập; sinh một ĐKCB mới cho tập đóng.</summary>
    [HttpPost("bindings")]
    [RequirePermission(PermissionCodes.SerialBind)]
    [ProducesResponseType(typeof(ApiResponse<SerialBindingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<SerialBindingDto>>> Bind(
        [FromBody] BindSerialIssuesCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);

        return Ok(Success(result,
            $"Đã đóng tập {result.Code} gồm {result.IssueCount} số, mã vạch {result.Barcode}."));
    }

    // ---------------------------------------------------------------
    // Báo cáo (IV.5)
    // ---------------------------------------------------------------

    /// <summary>Các chiều thống kê dùng được của phân hệ ấn phẩm định kỳ.</summary>
    [HttpGet("reports/dimensions")]
    [RequirePermission(PermissionCodes.SerialReportView)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyDictionary<string, string>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<IReadOnlyDictionary<string, string>>> Dimensions() =>
        Ok(Success(SerialDimensions.Labels));

    /// <summary>Thống kê ấn phẩm định kỳ: tổng hợp, theo môn loại, mức định kỳ, ngôn ngữ.</summary>
    [HttpPost("reports/statistics")]
    [RequirePermission(PermissionCodes.SerialReportView)]
    [ProducesResponseType(typeof(ApiResponse<SerialStatReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SerialStatReportDto>>> Statistics(
        [FromQuery] string dimension, [FromBody] SerialReportFilter filter, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetSerialStatsQuery(dimension, filter), ct);
        return Ok(Success(result));
    }

    /// <summary>Xuất báo cáo ấn phẩm định kỳ ra Excel hoặc PDF.</summary>
    [HttpPost("reports/export")]
    [RequirePermission(PermissionCodes.SerialReportView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportReport(
        [FromQuery] string dimension,
        [FromQuery] ExportFormat format,
        [FromBody] SerialReportFilter filter,
        CancellationToken ct)
    {
        var file = await Mediator.Send(new ExportSerialReportQuery(dimension, filter, format), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}

public class RespondClaimBody
{
    public string Response { get; set; } = string.Empty;
    public SerialClaimStatus Status { get; set; } = SerialClaimStatus.Responded;
}
